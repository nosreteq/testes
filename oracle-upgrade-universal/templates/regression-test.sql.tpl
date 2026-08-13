-- Template: comparação de resultado entre Oracle 19c e 23ai/26ai
-- Duas estratégias — use a que for viável no seu ambiente.

-- =====================================================================
-- ESTRATÉGIA A: DB LINK entre os dois ambientes (mais direto, se permitido)
-- =====================================================================
-- Pré-requisito: DB LINK criado do ambiente 26ai apontando para o 19c
-- (ou vice-versa). Rode a partir do ambiente que tiver o link.

-- Linhas presentes em 19c e ausentes em 26ai (o caso do bug de UNION real)
SELECT * FROM (
    {query_original}
    -- exemplo: SELECT col1, col2 FROM tabela WHERE ...
)
MINUS
SELECT * FROM (
    {query_original}@link_para_outro_ambiente
);

-- Linhas presentes em 26ai e ausentes em 19c (sentido inverso — confirma
-- que não é só reordenação, é diferença real de conjunto)
SELECT * FROM (
    {query_original}@link_para_outro_ambiente
)
MINUS
SELECT * FROM (
    {query_original}
);

-- Se as duas MINUS acima vierem vazias: resultado equivalente, sem
-- divergência de conjunto (pode ainda haver divergência de ordenação —
-- não é bug se a query não tem ORDER BY explícito).

-- =====================================================================
-- ESTRATÉGIA B: sem DB LINK — spool separado + diff externo
-- =====================================================================
-- Rode isto em cada ambiente separadamente, com o mesmo dado de entrada:

SET LINESIZE 32767
SET PAGESIZE 0
SET TRIMSPOOL ON
SET FEEDBACK OFF
SPOOL resultado_{ambiente}.txt

{query_original}
ORDER BY {chave_primaria_ou_colunas_deterministicas}; -- força ordem estável
                                                        -- só para fins de
                                                        -- comparação textual,
                                                        -- não altere a query
                                                        -- original testada

SPOOL OFF

-- Depois, fora do banco:
-- diff resultado_19c.txt resultado_26ai.txt

-- =====================================================================
-- Captura de plano de execução (para instruir SR, se houver divergência)
-- =====================================================================
EXPLAIN PLAN FOR
{query_original};

SELECT * FROM TABLE(DBMS_XPLAN.DISPLAY);
