# Catálogo de Comandos/Recursos Obsoletos — Oracle 19c → 23ai/26ai

Este arquivo é **vivo**: a Fase 2 (Deep Research) acrescenta entradas
específicas do seu schema. As entradas abaixo já são confirmadas em
documentação oficial Oracle (levantadas nesta sessão de criação do plugin,
ago/2026). Nota de contexto: **Oracle AI Database 26ai é o rebrand LTS de
23ai** — mesma linha de código, mesmos breaking changes acumulados desde 19c,
mais os específicos da 26ai. Upgrade direto 19c→26ai é suportado, via
AutoUpgrade.

## Como usar esta tabela
Para cada comando/pacote/sintaxe usado no seu código (levantado na Fase 1),
verifique se está aqui. Se não estiver, é isso que a Fase 2 precisa pesquisar
e adicionar — não assuma que "não está na lista" significa "está ok".

| Categoria | Item | Status | Desde | Substituto recomendado | Fonte |
|---|---|---|---|---|---|
| Package | `DBMS_OBFUSCATION_TOOLKIT` (inteiro, incl. `.MD5`, `.DESENCRYPT`) | **Removido** — chamada gera erro de compilação/runtime | 21c | `DBMS_CRYPTO` (ver linha abaixo sobre algoritmo) | docs.oracle.com/DBMS_CRYPTO |
| Algoritmo | `DBMS_CRYPTO` com `HASH_MD4` | **Desuportado** (não usar) | 26ai | `HASH_SH256` ou superior | docs.oracle.com/DBMS_CRYPTO |
| Algoritmo | `DBMS_CRYPTO` com `HASH_MD5` / SHA-1 (incl. SQLNET) | **Deprecated** (funciona, mas evitar em código novo) | 21c | `HASH_SH256` / `HASH_SH384` / `HASH_SH512` | docs.oracle.com/DBMS_CRYPTO |
| Package | `DBMS_LOCK.SLEEP` | **Deprecated** | não especificado nas notas consultadas | `DBMS_SESSION.SLEEP` | Oracle Deprecated/Desupported compilation |
| Package | `DBMS_XMLSTORE` | **Deprecated** em 26ai (pode ser desuportado em release futura) | 26ai | SQL DML padrão + SQL/XML, XQuery | docs.oracle.com — Changes in PL/SQL Packages Reference 26ai |
| Package | `DBMS_XMLGEN` | **Deprecated** em 26ai | 26ai | Operadores SQL/XML nativos | docs.oracle.com — Changes in PL/SQL Packages Reference 26ai |
| Auditoria | Traditional Auditing (parâmetros/pacotes `INIT_CLEANUP`, `DEINIT_CLEANUP`, `IS_CLEANUP_INITIALIZED`) | **Desuportado** para criação/alteração desde 23c; pacotes deprecated em 26ai | 23c / 26ai | Unified Auditing | docs.oracle.com — Development Guide Changes 26ai |
| Criptografia | `3DES` em modo FIPS 140-3 | **Desuportado** em 26ai (19c remove em RU 19.32, 2026) | 26ai | AES | docs.oracle.com/rnrdm 26ai |
| Java | `SQLJ` (embedding de SQL em Java) | **Deprecated** em 26ai | 26ai | JDBC (dynamic SQL, prepared statements, blocos PL/SQL) | docs.oracle.com — Java Developer's Guide 26ai |
| Arquitetura | Non-CDB (banco clássico sem PDB) | **Removido** | 21c | CDB/PDB obrigatório — se ainda está em non-CDB, converter *antes* ou *durante* o upgrade (AutoUpgrade faz isso) | docs.oracle.com/upgrd |
| Ferramenta | DBUA / upgrade manual | **Deprecated**, apenas modo Admin-Managed suportado a partir de 23ai | 21c / 23ai | AutoUpgrade | dev.to — Key Features No Longer Supported 23ai |
| Ferramenta | Data Recovery Advisor (DRA) — `LIST FAILURE`, `ADVISE FAILURE`, `REPAIR FAILURE` | **Removido**, sem substituto direto | 23ai | Procedimentos manuais de RMAN | dev.to — Key Features No Longer Supported 23ai |
| OLAP | Analytic Workspaces, OLAP DML, OLAP Java API, financial reporting | **Deprecated**, contínua em 26ai, sem garantia pós-26ai | 26ai | Migrar para modelagem relacional/analítica nativa | docs.oracle.com — PL/SQL Packages Changes 26ai |
| Standby | `DBMS_LOGSTDBY` com Extended Datatype Support (EDS) | **Deprecated** | 19c/21c/23ai | Tipos suportados nativamente por logical standby ou GoldenGate | visual-expert.com compilation |

## Riscos de comportamento silencioso (não são "removidos", mas mudam resultado)
Estes **não geram erro** — por isso são os mais perigosos. Adicione aqui
qualquer regressão de otimizador confirmada no seu ambiente (ex: o caso já
identificado de `UNION` retornando vazio enquanto `UNION ALL` retorna dados —
regressão de otimizador confirmada via SR, corrigida com SQL Patch).

| Sintoma | Contexto | Ação |
|---|---|---|
| `UNION` retorna vazio, `UNION ALL` retorna dados | Regressão de plano de execução confirmada em migração 19c→23ai/26ai | Abrir SR Oracle, aplicar SQL Patch enquanto aguarda fix; **tratar como padrão de risco**: qualquer `UNION`/`UNION ALL`/`INTERSECT`/`MINUS` crítico deve ser regression-testado na Fase 5, não só revisado estaticamente |
| _(a preencher pela Fase 2/5)_ | | |

## Novidades de SQL que podem (mas não precisam) substituir padrões antigos
Não são breaking changes, mas valem nota na Fase 3 se o plano quiser
modernizar em vez de só portar:
- Tipo `BOOLEAN` nativo em SQL (antes só em PL/SQL) — colunas/expressões que
  hoje usam `CHAR(1)`/`NUMBER(1)` como flag podem, opcionalmente, migrar.
- Eliminação da obrigatoriedade de `FROM DUAL` em alguns contextos.
- AI Vector Search — fora do escopo de uma migração de portabilidade pura,
  mas relevante se o roadmap incluir busca semântica.
