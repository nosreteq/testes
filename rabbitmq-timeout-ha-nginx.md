# RabbitMQ: `BlockingCell.GetValue` timeout após migração para arquitetura HA

> Análise técnica de falha na publicação de mensagens de registro de uso da WebAPI após escalar de 1 para 5 instâncias atrás de Nginx.

---

## Parte 1 — Questionamento reformulado

### Contexto

**Arquitetura anterior (estável):**

| Item | Valor |
|---|---|
| Instâncias de WebAPI | 1 |
| Load balancer | Nenhum |
| Message broker | RabbitMQ (instância única) |
| Protocolo de acesso ao broker | AMQP 0-9-1 / TCP 5672 |
| Client library | `RabbitMQ.Client` 5.x (.NET Framework) |

**Arquitetura nova (com falha):**

| Item | Valor |
|---|---|
| Instâncias de WebAPI | 5 |
| Load balancer | Nginx (reverse proxy HTTP nas portas 80/443) |
| Message broker | RabbitMQ — *mesmo broker/cluster* |
| Mudança na camada de mensageria | Nenhuma declarada |

### Sintoma

Ao publicar a mensagem de registro de uso da WebAPI no message broker:

```
Ocorreu um erro ao enviar mensagem de registro de uso da WebApi ao MessageBroker

Exception Message: The operation has timed out.
   at RabbitMQ.Util.BlockingCell.GetValue(TimeSpan timeout)
```

A falha **não ocorria** na arquitetura anterior. Surgiu somente após a migração para HA.

### Perguntas

1. `RabbitMQ.Util.BlockingCell.GetValue` indica timeout em qual camada — handshake de conexão, abertura de canal, declaração de topologia ou confirmação de publicação?
2. O Nginx participa do caminho AMQP? É necessária configuração adicional nele para RabbitMQ?
3. Quais recursos passam a ser disputados quando 5 instâncias publicam no mesmo broker que antes atendia 1?
4. O padrão de conexão/canal usado no código é seguro sob concorrência (`IConnection` vs. `IModel`)?
5. Qual sequência de diagnóstico permite isolar a causa raiz de forma determinística?

### Informações a coletar antes de responder

- [ ] Trecho de código que instancia `ConnectionFactory` / `CreateConnection` / `CreateModel`
- [ ] Tempo de vida dos objetos: escopo de requisição, singleton ou estático?
- [ ] `nginx -T` — existe bloco `stream`?
- [ ] `rabbitmq-diagnostics alarms`
- [ ] `rabbitmqctl list_connections | wc -l`
- [ ] `rabbitmqctl list_queues name messages consumers`
- [ ] Contagem de `TIME_WAIT` nos servidores de aplicação
- [ ] O consumidor da fila de registro de uso também foi escalado?

---

## Parte 2 — Análise e resolução

### Resposta direta

**Não.** O Nginx, do jeito que está descrito, não está no caminho do AMQP.

**FATO:** Nginx como reverse proxy HTTP (bloco `http`) opera em 80/443 para as 5 instâncias de WebAPI. A conexão WebAPI → RabbitMQ é AMQP em TCP/5672, saindo direto de cada instância. Só passa pelo Nginx se você configurou explicitamente o módulo `stream` na porta 5672 — o que é raro e normalmente não é feito nesse cenário.

Confirme em 30 segundos:

```bash
nginx -T | grep -A20 '^stream'          # existe bloco stream?
netstat -ano | findstr :5672            # de cada WebAPI: para onde vai a conexão?
```

Se não há bloco `stream`, o Nginx é uma pista falsa. O que mudou de verdade foi **1 → 5 instâncias publicando no mesmo broker**.

---

### O que `BlockingCell.GetValue` realmente significa

**FATO:** `RabbitMQ.Util.BlockingCell` é usado pelo `SimpleBlockingRpcContinuation` — o mecanismo que bloqueia a thread chamadora esperando a resposta síncrona do broker em operações RPC do AMQP: `connection.open`, `channel.open`, `queue.declare`, `exchange.declare`, `basic.qos`, `confirm.select`, `WaitForConfirms`. O default de `ContinuationTimeout` é **20 segundos**.

**FATO:** O namespace `RabbitMQ.Util` público indica client **5.x ou anterior** (na 6.x virou interno). Relevante para as APIs abaixo.

**INFERÊNCIA:** O erro não é "o broker está lento para aceitar a mensagem". É "uma operação de setup (abrir conexão ou canal) não recebeu resposta em 20s". Isso aponta para esgotamento de recurso, não para throughput de mensagem.

---

### Causas prováveis, em ordem

#### 1. Conexão (ou canal) criada por requisição — causa nº 1

**INFERÊNCIA (alta confiança):** o código provavelmente faz `factory.CreateConnection()` dentro do método que registra o uso. Com 1 instância isso passava despercebido. Com 5 instâncias × concorrência do Nginx, o handshake AMQP (TCP + auth + tune + open) satura o broker.

Verificar no broker:

```bash
rabbitmqctl list_connections name peer_host state channels | wc -l
rabbitmqctl list_channels connection number state | wc -l
rabbitmqctl status | grep -A5 file_descriptors
```

Se `connections` está na casa das centenas/milhares para 5 instâncias, é isso.

Verificar nos servidores de aplicação (Windows) — **exaustão de portas efêmeras**:

```powershell
netstat -an | Select-String "TIME_WAIT" | Measure-Object -Line
netsh int ipv4 show dynamicport tcp
```

Range default 49152–65535 (~16k portas) com `TcpTimedWaitDelay` de 240s. Conexão por publish a taxa moderada estoura isso, e o `connect` passa a bloquear até o timeout.

#### 2. Alarme de recurso no broker (flow control)

**FATO:** Quando o RabbitMQ atinge `vm_memory_high_watermark` (default 0.4 da RAM) ou `disk_free_limit`, ele emite `connection.blocked` e **para de ler frames das conexões publicadoras**. Qualquer RPC subsequente trava até o `ContinuationTimeout`. Sintoma idêntico ao relatado.

```bash
rabbitmq-diagnostics alarms
rabbitmq-diagnostics memory_breakdown
rabbitmqctl list_queues name messages messages_unacknowledged consumers state
```

Se a fila de registro de uso não tem consumidor ativo (ou o consumidor não escalou junto), 5x publish sem consumo = fila cresce = alarme de memória. **Isso é muito plausível dado que só a camada WebAPI foi escalada.**

#### 3. `IModel` compartilhado entre threads

**FATO:** `IConnection` é thread-safe. **`IModel` não é.** Publicar no mesmo `IModel` a partir de requisições concorrentes corrompe o frame sequencing e trava RPCs.

Se existe um `static IModel` ou um `IModel` num singleton DI, é bug garantido — e a concorrência maior por instância no ambiente HA fez ele aparecer.

#### 4. Vazamento de canais

`IModel` não descartado → `channel_max` (default 2047 por conexão) esgota → `channel.open` bloqueia → timeout.

#### 5. Heartbeat vs. firewall/LB entre app e broker

**FATO:** Se existe qualquer stateful firewall, NAT ou LB entre WebAPI e RabbitMQ, e `RequestedHeartbeat = 0` (desabilitado) ou muito alto, a conexão morre half-open. O próximo RPC bloqueia até o timeout. Ambientes HA quase sempre introduzem esse elemento.

---

### Correção: connection manager singleton com pool de canais

Substitua o código de publicação por isto (compatível com .NET Framework 4.7.2 + RabbitMQ.Client 5.x):

```csharp
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

public sealed class RabbitBroker : IDisposable
{
    private static readonly Lazy<RabbitBroker> _lazy =
        new Lazy<RabbitBroker>(() => new RabbitBroker(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static RabbitBroker Instance { get { return _lazy.Value; } }

    private readonly ConnectionFactory _factory;
    private readonly List<string> _hosts;
    private readonly object _gate = new object();
    private readonly ConcurrentBag<IModel> _pool = new ConcurrentBag<IModel>();
    private readonly SemaphoreSlim _slots;
    private IConnection _connection;
    private bool _disposed;

    private const int MaxChannels = 32;

    private RabbitBroker()
    {
        _hosts = new List<string>(
            (Environment.GetEnvironmentVariable("RABBITMQ_HOSTS") ?? "localhost")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

        _slots = new SemaphoreSlim(MaxChannels, MaxChannels);

        _factory = new ConnectionFactory
        {
            UserName    = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
            Password    = Environment.GetEnvironmentVariable("RABBITMQ_PASS"),
            VirtualHost = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/",
            Port        = 5672,

            // Recuperação automática — essencial em HA
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled  = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(5),

            // Heartbeat curto: detecta conexão half-open antes do RPC travar.
            // 5.x: ushort em segundos. 6.x: TimeSpan.FromSeconds(20).
            RequestedHeartbeat = 20,

            RequestedConnectionTimeout = 10000,              // ms, 5.x
            ContinuationTimeout        = TimeSpan.FromSeconds(15),
            RequestedChannelMax        = MaxChannels * 2,

            // Identifica a instância no rabbitmqctl list_connections
            ClientProvidedName = string.Format("webapi-{0}-{1}",
                Environment.MachineName, System.Diagnostics.Process.GetCurrentProcess().Id)
        };
    }

    private IConnection Connection
    {
        get
        {
            var c = _connection;
            if (c != null && c.IsOpen) return c;

            lock (_gate)
            {
                if (_connection != null && _connection.IsOpen) return _connection;

                if (_connection != null)
                {
                    try { _connection.Dispose(); } catch { /* ignore */ }
                    DrainPool();
                }

                // Failover entre nós do cluster
                _connection = _factory.CreateConnection(_hosts, _factory.ClientProvidedName);
                _connection.ConnectionShutdown += (s, e) => DrainPool();
                _connection.ConnectionBlocked  += (s, e) =>
                    Log.Warn("RabbitMQ bloqueou publish. Motivo: " + e.Reason);
                return _connection;
            }
        }
    }

    private void DrainPool()
    {
        IModel ch;
        while (_pool.TryTake(out ch))
        {
            try { ch.Dispose(); } catch { /* ignore */ }
        }
    }

    /// <summary>
    /// Publica de forma fire-and-forget com confirmação. Nunca lança para o caller:
    /// registro de uso não pode derrubar a requisição HTTP.
    /// </summary>
    public bool TryPublish(string exchange, string routingKey, byte[] body,
                           string messageId, TimeSpan confirmTimeout)
    {
        if (!_slots.Wait(TimeSpan.FromSeconds(2)))
        {
            Log.Warn("Pool de canais RabbitMQ saturado; mensagem descartada.");
            return false;
        }

        IModel channel = null;
        bool reusable = false;
        try
        {
            if (!_pool.TryTake(out channel) || !channel.IsOpen)
            {
                if (channel != null) { try { channel.Dispose(); } catch { } }
                channel = Connection.CreateModel();
                channel.ConfirmSelect();
            }

            var props = channel.CreateBasicProperties();
            props.Persistent  = true;
            props.MessageId   = messageId;              // idempotência a jusante
            props.ContentType = "application/json";
            props.Timestamp   = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(exchange, routingKey, mandatory: false,
                                 basicProperties: props, body: body);

            channel.WaitForConfirmsOrDie(confirmTimeout);
            reusable = true;
            return true;
        }
        catch (OperationInterruptedException ex)
        {
            Log.Error("RabbitMQ interrompeu a operação: " + ex.Message, ex);
            return false;
        }
        catch (Exception ex)   // TimeoutException, BrokerUnreachable, AlreadyClosed
        {
            Log.Error("Falha ao publicar registro de uso: " + ex.Message, ex);
            return false;
        }
        finally
        {
            if (channel != null)
            {
                if (reusable && channel.IsOpen) _pool.Add(channel);
                else { try { channel.Dispose(); } catch { } }
            }
            _slots.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DrainPool();
        if (_connection != null) { try { _connection.Dispose(); } catch { } }
        _slots.Dispose();
    }
}
```

Pontos que resolvem o sintoma:

- **Uma conexão TCP por processo** (5 no total, não milhares). Elimina exaustão de portas efêmeras e pressão de handshake no broker.
- **Um `IModel` por operação concorrente**, nunca compartilhado — respeita a não-thread-safety.
- **Heartbeat de 20s** derruba conexão half-open antes de um RPC travar 20s.
- **Falha isolada**: registro de uso é telemetria; nunca deve propagar exceção para a requisição do usuário.
- **`ClientProvidedName`** identifica qual das 5 instâncias está causando problema no `list_connections`.

Se você usa .NET Framework 4.7.2, atenção a `DateTimeOffset.ToUnixTimeSeconds()` — disponível desde 4.6, ok.

---

### Se o RabbitMQ *estiver* atrás do Nginx (bloco `stream`)

Nesse caso há sim configuração obrigatória:

```nginx
stream {
    upstream rabbitmq_amqp {
        least_conn;
        server 10.0.0.11:5672 max_fails=2 fail_timeout=10s;
        server 10.0.0.12:5672 max_fails=2 fail_timeout=10s;
        server 10.0.0.13:5672 max_fails=2 fail_timeout=10s;
    }

    server {
        listen 5672;
        proxy_pass rabbitmq_amqp;

        proxy_connect_timeout   5s;
        proxy_timeout           3600s;   # CRÍTICO: > intervalo de heartbeat
        proxy_socket_keepalive  on;
    }
}
```

**FATO:** o `proxy_timeout` default do módulo `stream` é **10 minutos** de ociosidade. Conexão AMQP parada por 10min é fechada silenciosamente pelo Nginx → o cliente só descobre no próximo RPC → `BlockingCell` timeout. Com heartbeat de 20s isso não ocorre (o tráfego de heartbeat mantém a conexão ativa), mas manter `proxy_timeout` alto é defesa em profundidade.

Ainda assim: **não recomendo** colocar AMQP atrás do Nginx. O client .NET já faz failover nativo via `CreateConnection(IList<string> hosts)` e o Nginx apenas adiciona um ponto de falha e um intermediário que pode matar conexões.

---

### Ordem de investigação sugerida

1. `rabbitmq-diagnostics alarms` — se retornar alarme, é flow control e o resto é consequência.
2. `rabbitmqctl list_connections | wc -l` — se >> 5, é conexão por requisição.
3. `netstat -an | findstr TIME_WAIT | find /c ":"` em uma WebAPI — se > 10.000, exaustão de portas.
4. `rabbitmqctl list_queues name messages consumers` — consumidor da fila de uso escalou junto? Se `consumers = 0` com `messages` crescendo, achou a causa raiz.

**Premissa declarada:** assumi que o RabbitMQ é o mesmo broker (ou cluster) nas duas estruturas e que a mudança foi só na camada de aplicação. Se o RabbitMQ também virou cluster nessa migração, o item 4 acima ganha peso — filas classic mirrored com sincronização pendente travam `queue.declare` exatamente com esse erro.
