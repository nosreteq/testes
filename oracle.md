# Tabelas e views de configuração Oracle: comparar 19c x 23ai para diagnosticar UNION sem retorno

Além de `ALL_TAB_COLUMNS`, existem várias outras camadas de configuração que podem explicar por que o `UNION` se comporta diferente entre 19c e 23ai — cada uma cobre uma hipótese diferente das já discutidas anteriormente (NLS, otimizador, fix control, etc.).

## 1. NLS — a camada mais provável

```sql
-- Nível banco (define default de toda sessão nova)
SELECT parameter, value FROM nls_database_parameters
WHERE parameter IN ('NLS_CHARACTERSET','NLS_NCHAR_CHARACTERSET',
                    'NLS_COMP','NLS_SORT','NLS_LENGTH_SEMANTICS','DEFAULT_COLLATION');

-- Nível instância (spfile — o que cada sessão herda ao conectar)
SELECT name, value, isdefault FROM v$parameter
WHERE name LIKE 'nls%' OR name = 'default_collation';

-- Compare 19c x 23ai via dblink
SELECT a.parameter, a.value AS valor_19c, b.value AS valor_23ai
FROM   nls_database_parameters@lnk19c a
JOIN   nls_database_parameters b ON a.parameter = b.parameter
WHERE  NVL(a.value,'x') <> NVL(b.value,'x');
```

Se `NLS_COMP`/`NLS_SORT`/`DEFAULT_COLLATION` divergirem aqui, é a causa mais provável — já discutido anteriormente, mas essa é a fonte oficial de onde vem o default (spfile/logon trigger), não só o que a sessão herdou.

## 2. Fix Control — bugs/comportamentos ligados a versão, ativáveis/desativáveis individualmente

```sql
SELECT bugno, description, value, session_settable
FROM   v$system_fix_control
WHERE  UPPER(description) LIKE '%SET%' 
    OR UPPER(description) LIKE '%UNION%'
    OR UPPER(description) LIKE '%SORT%'
ORDER BY bugno;

-- O que está ativo na SUA sessão especificamente
SELECT bugno, value, session_value FROM v$session_fix_control
WHERE sid = SYS_CONTEXT('USERENV','SID');
```

Isso é literalmente o mecanismo por trás do `OPTIMIZER_FEATURES_ENABLE` — cada bugfix do otimizador tem um `_fix_control` individual. Se algo aqui, relacionado a `SET`/`UNION`/`SORT`, estiver habilitado por padrão no 23ai e não existir (ou default diferente) no 19c, é o candidato mais cirúrgico de todos — permite corrigir só aquele fix em vez de rebaixar o `OFE` inteiro.

## 3. Parâmetros ocultos (`_underscore`) relacionados a set operators

```sql
SELECT ksppinm AS parametro, ksppstvl AS valor, ksppdesc AS descricao
FROM   x$ksppi  i, x$ksppcv v
WHERE  i.indx = v.indx
AND    ksppinm LIKE '\_%union%' ESCAPE '\'
ORDER  BY ksppinm;
```

(requer SYS ou privilégio explícito na x$; peça ao DBA se não tiver acesso)

## 4. Optimizer parameters gerais — plano diferente pode não ser só OFE

```sql
SELECT name, value FROM v$parameter WHERE name LIKE 'optimizer%';
```

Compare linha a linha com o 19c via dblink. `optimizer_adaptive_plans`, `_optimizer_use_feedback`, `optimizer_dynamic_sampling` mudaram de default entre versões e podem interagir com o bug.

## 5. Character set das colunas NCHAR/NVARCHAR2, se aplicável

```sql
SELECT parameter, value FROM nls_database_parameters WHERE parameter = 'NLS_NCHAR_CHARACTERSET';
```

Se a tabela usar `NVARCHAR2`/`NCHAR`, o charset nacional é independente do `NLS_CHARACTERSET` e pode ter divergido separadamente na migração.

## 6. Plan Baselines / SQL Patches / Profiles já existentes (podem estar interferindo sem você saber)

```sql
SELECT sql_handle, plan_name, enabled, accepted, origin
FROM   dba_sql_plan_baselines
WHERE  sql_text LIKE '%<trecho da sua query>%';

SELECT name, sql_text, status FROM dba_sql_patches;
```

Se um Plan Baseline de uma versão anterior estiver "preso" e conflitando com o plano nativo do 23ai, isso pode gerar comportamento incoerente entre execuções — vale eliminar essa hipótese.

## 7. Character set / collation ao nível de sessão vs objeto

```sql
-- Já mencionado, mas incluindo aqui para completude do checklist:
SELECT * FROM nls_session_parameters;
```

## Ordem recomendada de investigação

Do mais provável ao mais raro no cenário de migração 19c → 23ai:

1. `nls_database_parameters` comparado 19c x 23ai (NLS_COMP/SORT/COLLATION)
2. `v$system_fix_control` filtrando SET/UNION/SORT
3. Optimizer parameters gerais
4. Plan Baselines/SQL Patches pré-existentes
5. Parâmetros `_underscore` ocultos

Os itens 1 e 2 são os que, historicamente, mais explicam esse tipo de divergência de comportamento entre versões — vale começar por eles.
