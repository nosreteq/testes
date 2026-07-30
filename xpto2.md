# Correção: caracteres invisíveis ou homóglifos no campo XPTO pós-tombamento Oracle 19c → 23ai

Antes de corrigir, vale fechar o diagnóstico — porque a hipótese tem um detalhe lógico importante:

**FATO:** a conversão de charset em si (WE8MSWIN1252/ISO8859 → AL32UTF8) preserva bytes 0x00–0x7F intactos. Um `'A'` limpo (0x41) no 19c **jamais** vira homóglifo ou ganha NBSP só pela conversão do banco.

**INFERÊNCIA:** se a query zerou completamente (nenhuma linha retorna), ou a sujeira foi introduzida pela ferramenta do tombamento (arquivo intermediário CSV/Excel, ETL com NLS_LANG errado no client, mapeamento do GoldenGate/SQL*Loader), ou a causa real é outra (NLS_COMP ou CHAR padding — causas 1 e 2 da análise anterior). E se a coluna for `CHAR(1 BYTE)`/`VARCHAR2(1 BYTE)`, homóglifo é fisicamente impossível (multibyte não caberia — daria ORA-12899 na carga).

## 1. Confirme com inventário físico

Coluna flag tem baixa cardinalidade, então isso roda em segundos e mostra exatamente quais bytes existem:

```sql
SELECT XPTO,
       DUMP(XPTO, 1016) AS bytes_hex,
       COUNT(*)         AS qtd
FROM   sua_tabela
GROUP  BY XPTO, DUMP(XPTO, 1016)
ORDER  BY qtd DESC;
```

Se aparecer `ce,91` (Α grego), `d0,90` (А cirílico), `ef,bc,a1` (Ａ fullwidth), `c2,a0` (NBSP), `e2,80,8b` (zero-width) etc., hipótese confirmada — e você sabe exatamente o que limpar.

## 2. Backup das linhas afetadas

Recuperação barata sem depender de flashback:

```sql
CREATE TABLE sua_tabela_bkp_xpto AS
SELECT ROWID AS rid_orig, XPTO
FROM   sua_tabela
WHERE  XPTO <> ASCIISTR(XPTO)              -- contém não-ASCII
    OR REGEXP_LIKE(XPTO, '[[:cntrl:]]')    -- TAB/CR/LF e controles
    OR XPTO <> TRIM(XPTO);                 -- espaços nas pontas
```

## 3. Correção definitiva — UPDATE com TRANSLATE

Mapeia homóglifos → `A` e remove invisíveis. **Ajuste a lista conforme os bytes reais que o DUMP mostrou** — não deixe caractere de fora nem inclua o que não existe:

```sql
UPDATE sua_tabela
SET    XPTO = TRIM(TRANSLATE(XPTO,
                  UNISTR('\0391\0410\FF21')                        -- Α grego, А cirílico, Ａ fullwidth → 'A'
               || UNISTR('\00A0\2007\202F\200B\200C\200D\FEFF')    -- NBSP e variantes, zero-width, BOM → remove
               || CHR(9) || CHR(10) || CHR(13),                    -- TAB, LF, CR → remove
                  'AAA'))
WHERE  XPTO <> ASCIISTR(XPTO)
    OR REGEXP_LIKE(XPTO, '[[:cntrl:]]')
    OR XPTO <> TRIM(XPTO);
```

Semântica do TRANSLATE: os 3 primeiros caracteres do from-string mapeiam para `'AAA'`; todos os demais (sem correspondente no to-string) são removidos.

Dois cuidados:

- Linha que era só sujeira vira string vazia = **NULL** no Oracle.
- Para volume grande (milhões de linhas), quebre em lotes ou use `DBMS_PARALLEL_EXECUTE` para controlar undo/redo.

## 4. Validação antes do COMMIT

```sql
-- Deve voltar a preencher
SELECT COUNT(*) FROM sua_tabela WHERE XPTO = 'A';

-- Não deve sobrar nada
SELECT COUNT(*) FROM sua_tabela
WHERE  XPTO <> ASCIISTR(XPTO)
    OR REGEXP_LIKE(XPTO, '[[:cntrl:]]')
    OR XPTO <> TRIM(XPTO);
```

Conferiu, `COMMIT`.

## Workaround imediato

Leitura em produção sem poder alterar dados ainda:

```sql
WHERE TRANSLATE(XPTO, UNISTR('\0391\0410\FF21\00A0\200B\FEFF'), 'AAA') = 'A'
```

Isso mata o uso de índice em XPTO (full scan) — serve como paliativo, não como solução permanente.

## 5. Origem

Se o tombamento será reexecutado ou há carga contínua alimentando a tabela, corrigir só o dado atual não resolve: valide o NLS_LANG do client de carga e a etapa intermediária do ETL que introduziu esses bytes — senão a sujeira volta na próxima carga.
