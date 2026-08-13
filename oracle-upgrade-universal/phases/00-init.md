# Fase 0 — Inicialização

Você é o agente orquestrador desta sessão de migração Oracle.

## Instruções

1. Pergunte ao usuário:
   - Versão de origem (ex: `19c`, incluir Release Update se souber, ex: `19.24`)
   - Versão alvo (ex: `23ai`, `26ai`)
   - Escopo: schema(s) específico(s) ou banco inteiro
   - Se há ambiente de staging/26ai acessível para validação real (Fase 5) ou
     se a validação será só estática

2. Crie o diretório de sessão:
   `<solution-root>/.oracle-upgrade/up-19c-to-26ai-{timestamp}/`

3. Se `<solution-root>/.oracle-upgrade/INDEX.md` não existir, crie-o:
   ```markdown
   # Índice de Sessões de Migração Oracle
   | Sessão | Origem | Alvo | Escopo | Status | Data |
   |---|---|---|---|---|---|
   ```

4. Adicione uma linha em `INDEX.md`, status `em andamento`.

5. Crie `model-selection.md` na sessão (consulte `config/models.md`):
   ```markdown
   # Seleção de Modelo por Fase — sessão up-19c-to-26ai-{timestamp}
   | Fase | Modelo | Observação |
   |---|---|---|
   | 1. Inventário | {a definir} | acesso ao dicionário de dados ajuda |
   | 2. Deep Research | {a definir} | precisa web search |
   | 3. Plano de Validação | {a definir} | |
   | 4. Breakdown | {a definir} | |
   | 5. Execução | {a definir} | idealmente com acesso a SQL real |
   | 6. Resultado Final | {a definir} | |
   ```

6. Confirme ao usuário o caminho da sessão e **pare**.

## Saída esperada
- Diretório de sessão criado
- `INDEX.md` atualizado
- `model-selection.md` preenchido
