# Plugin: Migração Oracle 19c → 26ai — Multi-Model (Model-Agnostic)

## Propósito
Mesmo padrão do plugin de modernização .NET, adaptado para migração de banco
Oracle 19c → 23ai/26ai (mesma linha de código — 26ai é o rebrand LTS de 23ai
com AI Vector Search e commitment de Long Term Support). Cada fase é um `.md`
autocontido que você cola no modelo escolhido. Sem chamada de API embutida;
o roteamento de modelo fica em `model-selection.md` por sessão.

## Diferença crítica em relação à modernização .NET
Migração de código quebra em **build time** — você vê o erro na hora.
Migração de Oracle quebra em **runtime, silenciosamente**: query compila,
executa, devolve resultado errado ou incompleto, sem exception. Por isso este
plugin tem uma fase que a versão .NET não tem: **validação de equivalência de
resultado** (Fase 5), comparando output entre 19c e 26ai para o mesmo comando,
não só "compilou sem erro".

## Pré-requisitos de capacidade do modelo/agente por fase
| Capacidade | Necessária em |
|---|---|
| Acesso de leitura ao dicionário de dados (`DBA_*`/`ALL_*` views) ou export de DDL | Fase 1 |
| Busca web | Fase 2 (obrigatória) |
| Execução de SQL contra ambiente 19c e 26ai (ou staging) | Fase 5 (fortemente recomendado — sem isso a validação é só estática) |

## Como usar
1. Preencha `config/models.md` com os modelos disponíveis.
2. Consulte `config/catalogo-obsolescencia.md` — já vem populado com achados
   reais de Oracle 19c→23ai/26ai (breaking changes confirmados em documentação
   oficial). A Fase 2 expande esse catálogo com o que for específico do seu
   schema.
3. Abra o modelo escolhido para a Fase 0 e cole `phases/00-init.md`.
4. Siga a sequência abaixo.

## Sequência de fases

| # | Fase | Arquivo | Modelo sugerido | Paralelizável |
|---|---|---|---|---|
| 0 | Inicialização | `phases/00-init.md` | qualquer | Não |
| 1 | Inventário de Objetos | `phases/01-inventario.md` | qualquer c/ acesso ao dicionário | Não |
| 2 | Deep Research (Obsolescência) | `phases/02-pesquisa-obsolescencia.md` | modelo c/ web search | Não |
| 3 | Plano de Validação | `phases/03-plano-validacao.md` | modelo forte em raciocínio | Não |
| 4 | Breakdown por Objeto | `phases/04-breakdown.md` | qualquer | Não |
| 5 | Execução (validação + correção) | `phases/05-execucao-task.md` | modelo forte em codegen SQL/PLSQL | **Sim**, 1 por objeto/grupo |
| 6 | Resultado Final | `phases/06-resultado-final.md` | qualquer | Não |

## Escopo de objetos cobertos (Fase 1 e 5)
- DDL: tabelas, views, materialized views, índices, constraints, sequences, tipos
- PL/SQL: packages (spec+body), procedures, functions, triggers standalone
- Comandos internos: chamadas a packages built-in da Oracle (`DBMS_*`, `UTL_*`)
- CRUD de aplicação: INSERT/UPDATE/DELETE/MERGE, especialmente os que dependem
  de comportamento implícito (ordenação, NLS, coerção de tipo)
- Jobs agendados (`DBMS_SCHEDULER`), DB links, sinônimos

## Artefatos gerados

```
<solution-root>/.oracle-upgrade/
  INDEX.md
  up-19c-to-26ai-{timestamp}/
    model-selection.md
    01-inventario.md
    02-obsolescencia.md
    03-plano-validacao.md
    tasks/
      <objeto_ou_grupo>.task.md
      <objeto_ou_grupo>.result.md
    04-resultado-final.md
```
