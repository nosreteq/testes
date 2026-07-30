>> a consulta que fazia no oracle 19c  usando o filtro where no campo XPTO = 'A'  trazia o record set preenchido, porem ao fazer a mesma consulta no oracle 23ai  com where o campo XPTO = 'A'  não retorna nada, por que?

# Diagnóstico: query com `WHERE XPTO = 'A'` retorna dados no Oracle 19c mas nada no 23ai

A query em si não mudou de semântica entre 19c e 23ai — o que muda numa migração é o **conteúdo físico dos dados**, o **tipo da coluna** ou os **parâmetros NLS de comparação**. O diagnóstico definitivo é olhar byte a byte o que está armazenado:

```sql
-- 1. O que existe fisicamente no campo (encerra a discussão)
SELECT XPTO,
       DUMP(XPTO, 1016)  AS bytes_hex,
       LENGTH(XPTO)      AS len_chars,
       LENGTHB(XPTO)     AS len_bytes
FROM   sua_tabela
WHERE  UPPER(TRIM(XPTO)) = 'A';

-- 2. Parâmetros de comparação (sessão e banco)
SELECT parameter, value FROM nls_session_parameters
WHERE  parameter IN ('NLS_COMP','NLS_SORT');

SELECT parameter, value FROM nls_database_parameters
WHERE  parameter IN ('NLS_COMP','NLS_SORT','NLS_CHARACTERSET');

-- 3. Tipo real da coluna no 23ai
SELECT column_name, data_type, data_length, char_used
FROM   user_tab_columns
WHERE  table_name = 'SUA_TABELA' AND column_name = 'XPTO';
```

## Causas em ordem de probabilidade numa migração 19c → 23ai

### 1. NLS_COMP / NLS_SORT divergentes (case sensitivity)

Se o `DUMP` mostrar `61` (hex de `'a'` minúsculo): o ambiente 19c provavelmente rodava com `NLS_COMP=LINGUISTIC` + `NLS_SORT=BINARY_CI` (via trigger de logon, spfile ou config do client), fazendo `'a' = 'A'` ser verdadeiro. O 23ai instalado com defaults (`BINARY`) quebra isso silenciosamente — zero rows, sem erro.

Teste rápido:

```sql
ALTER SESSION SET NLS_COMP = LINGUISTIC;
ALTER SESSION SET NLS_SORT = BINARY_CI;
-- rode a query original; se voltar a retornar, é isso
```

### 2. Blank padding — coluna mudou de CHAR para VARCHAR2

Se o `DUMP` mostrar `41,20,20` (`'A '` com espaços): no 19c a coluna era `CHAR(n)` e a comparação com literal usava semântica *blank-padded* — os espaços eram transparentes. Se a ferramenta de migração converteu para `VARCHAR2(n)`, os trailing spaces viraram parte do valor e `= 'A'` falha.

Confirme com a query 3 comparando os dicionários dos dois bancos.

Teste:

```sql
WHERE XPTO LIKE 'A%'
-- ou
WHERE TRIM(XPTO) = 'A'
```

### 3. Charset AL32UTF8 + caractere invisível ou homóglifo

Migração de WE8MSWIN1252/WE8ISO8859P1 para AL32UTF8 não altera o `'A'` ASCII (`41`), mas expõe sujeira que antes passava:

- NBSP (`c2,a0`), tab, CR/LF
- Homóglifos visualmente idênticos ao 'A' latino: `ce,91` é o Α grego, `d0,90` é o А cirílico

Só o `DUMP` revela.

### 4. Contexto/objeto errado

23ai é CDB/PDB obrigatório — a conexão pode estar caindo em outro PDB, ou um sinônimo apontando para schema diferente, ou os dados simplesmente não foram carregados.

Sanity check antes de tudo:

```sql
SELECT COUNT(*) FROM sua_tabela;

SELECT SYS_CONTEXT('USERENV','CON_NAME')       AS pdb,
       SYS_CONTEXT('USERENV','CURRENT_SCHEMA') AS schema_atual
FROM   dual;
```

### 5. Política de segurança filtrando linhas

Menos provável, mas VPD ou SQL Firewall novo no 23ai pode filtrar silenciosamente:

```sql
SELECT * FROM dba_policies WHERE object_name = 'SUA_TABELA';
```

## Resumo prático

Rode o `DUMP` primeiro. Ele resolve a ambiguidade entre os cenários 1, 2 e 3 em uma única query — o hex diz exatamente se o problema é:

| Byte no DUMP | Problema |
|---|---|
| `61` | Case (`'a'` minúsculo, NLS_COMP/NLS_SORT) |
| `41,20,...` | Trailing spaces (CHAR → VARCHAR2) |
| Byte não-ASCII (`c2,a0`, `ce,91`, `d0,90`...) | Caractere invisível ou homóglifo pós-conversão de charset |
