# Registro de Modelos

Preencha antes de iniciar uma sessão. A Fase 0 lê este arquivo para propor o
preenchimento de `model-selection.md` da sessão.

| model_id | provider | invocação | context window | pontos fortes | usar em |
|---|---|---|---|---|---|
| sonnet-4-6 | Anthropic | Claude Code / API | 200K | raciocínio + SQL/PLSQL consistente, web search nativa | pesquisa, plano, execução |
| mai-code | _(preencher)_ | _(preencher)_ | _(preencher)_ | _(preencher)_ | _(preencher)_ |
| gpt-5.6-luna | _(preencher)_ | _(preencher)_ | _(preencher)_ | _(preencher)_ | _(preencher)_ |

## Observação específica para este plugin
Para a Fase 5 (execução/validação), prefira um modelo que você consiga
acoplar a uma sessão SQL real (via ferramenta de execução, MCP de banco, ou
cole-e-cole manual do resultado). Validação puramente estática de PL/SQL
pega breaking changes de sintaxe, mas não pega regressão de otimizador —
esse tipo de bug (ex: UNION retornando vazio por regressão de plano) só
aparece comparando resultado real entre 19c e 26ai.
