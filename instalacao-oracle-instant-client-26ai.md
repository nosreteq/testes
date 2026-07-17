# Instalação e Configuração do Oracle Instant Client 23ai/26ai no Windows

**Guia em 9 etapas** — adaptado do vídeo de Márcio Mandarino ([link](https://www.youtube.com/watch?v=wqhWZVTIyzw), gravado com o 21c) e atualizado para a linha **23ai (23.26.x.x)**, a versão atualmente disponível para download e recomendada pela Oracle para conectar em bancos **23ai e 26ai** (Oracle AI Database).

Ferramentas abordadas: **Instant Client (Basic)**, **SQL*Plus**, **SQLcl** e **SQL Developer**.

> **Notas específicas do 26ai:**
> - Não existe Instant Client **32-bit** para 26ai. Se precisar de 32-bit (ex.: aplicação legada), use o Instant Client **19c 32-bit**, que conecta normalmente em 26ai.
> - A partir do Client 26ai, o Setup Wizard do full client **não instala mais o Instant Client** — a instalação é exclusivamente via arquivos zip (exatamente o método deste guia).
> - Interoperabilidade client-server: Doc ID 207303.1 (MOS).

---

## Etapa 1 — Download dos recursos

Baixe os seguintes pacotes, todos para **Windows x64**:

| Recurso | Pacote | Observação |
|---|---|---|
| Instant Client | **Basic Package** (23.26.x.x) | Núcleo do client — página *Instant Client for Microsoft Windows (x64)* |
| SQL*Plus | **SQL*Plus Package** (23.26.x.x) | Pacote adicional do Instant Client |
| SQLcl | Versão mais recente | Download separado |
| SQL Developer | Versão **com JDK incluso** | Evita dependência de Java instalado |
| **VC++ Redistributable** | Visual Studio C++ Redistributable **mais recente** (x64) | **Obrigatório** — o Instant Client 23ai/26ai não roda sem ele |

> **Dica:** baixe sempre a versão mais recente do client (23.26.x.x), mesmo que o servidor rode 19c, 21c ou 23ai. O client novo é retrocompatível e evita problemas futuros. A Oracle removerá as versões antigas do site de downloads.

---

## Etapa 2 — Criação da estrutura de diretórios

```
C:\Oracle\           ← diretório raiz
C:\Oracle\scripts\   ← scripts SQL pessoais (usado pelo SQLPATH)
C:\Oracle\Tools\     ← SQLcl e SQL Developer
```

O caminho pode ser ajustado para outra partição/disco, desde que os apontamentos das variáveis de ambiente sejam coerentes.

---

## Etapa 3 — Organização e descompactação dos recursos

1. **Instale o VC++ Redistributable** baixado na Etapa 1 (pré-requisito de runtime).
2. Mova **SQL Developer** e **SQLcl** para `C:\Oracle\Tools\` e descompacte ambos **diretamente na pasta** (desmarque a criação de subdiretório adicional no descompactador).
3. Mova o **Basic Package** e o **SQL*Plus Package** para `C:\Oracle\` e descompacte. Ambos extraem para o **mesmo diretório** `instantclient_23_26`.
   - Alternativa via terminal (Windows recente):
     ```cmd
     cd C:\Oracle
     tar -xf instantclient-basic-windows.x64-23.26.0.0.0.zip
     tar -xf instantclient-sqlplus-windows.x64-23.26.0.0.0.zip
     ```
   - **Atenção (bug conhecido):** não use `unzip -t` para testar os zips do Instant Client 23ai no Windows — a verificação falha. Descompacte direto.
4. Crie o subdiretório de configuração de rede, se não existir: `C:\Oracle\instantclient_23_26\network\admin`.
5. Apague os arquivos `.zip` após a extração.

Estrutura final esperada:

```
C:\Oracle\instantclient_23_26\                 ← Basic + SQL*Plus (ORACLE_HOME)
C:\Oracle\instantclient_23_26\network\admin\   ← tnsnames.ora
C:\Oracle\scripts\
C:\Oracle\Tools\sqlcl\bin\
C:\Oracle\Tools\sqldeveloper\
```

---

## Etapa 4 — Configuração das variáveis de ambiente

Menu Iniciar → **"Variáveis de ambiente"** → *Editar as variáveis de ambiente do sistema* → botão **Variáveis de Ambiente**. Crie em **Variáveis do sistema**:

| Variável | Valor | Finalidade |
|---|---|---|
| `ORACLE_HOME` | `C:\Oracle\instantclient_23_26` | Localização do client |
| `TNS_ADMIN` | `C:\Oracle\instantclient_23_26\network\admin` | Localização do tnsnames.ora |
| `SQLPATH` | `C:\Oracle\scripts` | Diretório de scripts executáveis via `@nome` |

Edite também a variável **`Path`** e adicione **duas entradas novas**:

1. `%ORACLE_HOME%` (ou `C:\Oracle\instantclient_23_26`)
2. `C:\Oracle\Tools\sqlcl\bin`

> **Importante:** se houver outras versões de bibliotecas Oracle instaladas na máquina (ex.: um client 19c/21c antigo), a entrada do 23.26 deve vir **antes** no `Path` — senão a aplicação carrega as DLLs erradas.

Clique **OK** para salvar tudo e **abra um novo terminal** (o `Path` não é recarregado em janelas já abertas).

---

## Etapa 5 — Teste inicial das ferramentas no terminal

```cmd
sqlplus        ← deve exibir a versão 23.x
sql            ← deve abrir o SQLcl (executável chama-se apenas "sql")
```

> Se `sqlplus` falhar com erro de DLL ausente (`VCRUNTIME140.dll` ou similar), o VC++ Redistributable não foi instalado — volte à Etapa 3.
> Neste ponto ainda **não é possível conectar** ao banco — falta o `tnsnames.ora`.

---

## Etapa 6 — Configuração do tnsnames.ora

1. Copie do **servidor** a entrada TNS desejada (ex.: `PDB1`).
2. Crie o arquivo **`tnsnames.ora`** em `C:\Oracle\instantclient_23_26\network\admin\` (confirme a troca de extensão quando o Windows perguntar).
3. Cole a entrada e salve. Exemplo:

```
PDB1 =
  (DESCRIPTION =
    (ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.68.130)(PORT = 1521))
    (CONNECT_DATA =
      (SERVER = DEDICATED)
      (SERVICE_NAME = PDB1)
    )
  )
```

> Ajuste **HOST**, **PORT** e **SERVICE_NAME** para o seu ambiente. Alternativamente, o client 23ai/26ai também aceita **EZConnect Plus** direto na conexão (`sqlplus usuario@//host:1521/PDB1`), dispensando o tnsnames para testes rápidos.

---

## Etapa 7 — Teste de conexão (SQL*Plus e SQLcl)

```cmd
sqlplus usuario@PDB1
```

Digite a senha quando solicitada e valide:

```sql
SELECT sysdate FROM dual;
SET LINES 1000
SELECT * FROM dba_users;
```

Mesmo teste no SQLcl: `sql usuario@PDB1`.

> **Segurança:** nunca passe a senha inline (`usuario/senha@PDB1`) em ambiente produtivo — ela fica exposta no histórico e na lista de processos.

**Ajuste de legibilidade no console (CMD):**

1. Título da janela → **Propriedades** → aba **Layout**.
2. Desmarque **"Ajustar a saída de texto ao redimensionar"**.
3. Largura do buffer de tela: `9999`.

Isso cria barra de rolagem horizontal e alinha as colunas — o SQLcl já formata melhor a saída por padrão.

---

## Etapa 8 — Teste do SQL Developer

1. Execute `C:\Oracle\Tools\sqldeveloper\sqldeveloper.exe`.
2. Nova conexão:
   - **Nome:** `PDB1`
   - **Usuário/senha:** credenciais do banco
   - **Tipo de conexão:** **TNS** → selecione `PDB1` (lida via `TNS_ADMIN`)
3. **Testar** → sucesso → **Salvar** e conectar.
4. Valide com um `SELECT` na planilha SQL.

---

## Etapa 9 — Execução dinâmica de scripts (SQLPATH)

Crie `C:\Oracle\scripts\teste.sql`:

```sql
SELECT instance_name, status FROM v$instance;
SELECT name, open_mode FROM v$database;
```

Com `SQLPATH` configurado, execute de qualquer sessão SQL*Plus/SQLcl:

```sql
@teste
```

**No SQL Developer:** Ferramentas → Preferências → Banco de Dados → Planilha → diretório de scripts → `C:\Oracle\scripts` → salvar. A partir daí `@teste` funciona também na planilha.

> **Dica de produtividade:** mantenha seu "canivete suíço" de scripts nesse diretório. Na linha de comando, o prompt é liberado imediatamente após cada `@script`.

---

## Compatibilidade client × servidor

| Client instalado | Conecta em 19c | Conecta em 21c | Conecta em 23ai | Conecta em 26ai |
|---|---|---|---|---|
| **23ai (23.26.x.x)** — este guia | ✅ | ✅ | ✅ | ✅ |
| 19c 32-bit (legado) | ✅ | ✅ | ✅ | ✅ |
| 21c (o do vídeo) | ✅ | ✅ | ✅ | ⚠️ verificar Doc ID 207303.1 |

---

## Resumo comparativo das ferramentas

| Ferramenta | Pontos fortes | Observações |
|---|---|---|
| **SQL*Plus** | Muito leve, ideal para scripts formatados, prompt ágil | Preferido para o dia a dia com scripts |
| **SQLcl** | Formatação superior, recursos modernos | Pode ser mais pesado em algumas versões |
| **SQL Developer** | GUI completa, "melhor amigo do DBA" | Ideal para exploração e desenvolvimento |

---

## Checklist final

- [ ] VC++ Redistributable (x64) mais recente instalado
- [ ] Basic + SQL*Plus descompactados em `C:\Oracle\instantclient_23_26`
- [ ] SQLcl e SQL Developer em `C:\Oracle\Tools\`
- [ ] `ORACLE_HOME`, `TNS_ADMIN`, `SQLPATH` criadas
- [ ] `Path` com `%ORACLE_HOME%` (antes de clients antigos) e `...\sqlcl\bin`
- [ ] `tnsnames.ora` criado em `network\admin`
- [ ] Conexão testada no SQL*Plus, SQLcl e SQL Developer
- [ ] Script executado via `@nome` (SQLPATH funcionando)

---

## Nota para aplicações .NET

Se a aplicação usa **ODP.NET Managed** (`Oracle.ManagedDataAccess` / `Oracle.ManagedDataAccess.Core`), o Instant Client **não é necessário** — o driver managed é 100% .NET e resolve TNS via `TNS_ADMIN` ou configuração própria. O Instant Client só é obrigatório para **ODP.NET Unmanaged (OCI)**, ODBC, Pro*C e ferramentas nativas como SQL*Plus.
