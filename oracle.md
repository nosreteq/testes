-- 1. Fora do conjunto esperado pt-BR (pega homóglifos, NBSP, zero-width, U+FFFD)
--    Permitidos: TAB/LF/CR, ASCII imprimível, acentuados Latin-1, º ª ° §
REGEXP_LIKE(col, UNISTR('[^\0009\000A\000D\0020-\007E\00A7\00AA\00B0\00BA\00C0-\00FF]'))

-- 2. Caracteres de controle
REGEXP_LIKE(col, '[[:cntrl:]]')

-- 3. Bytes inválidos para AL32UTF8 (round-trip muda o valor)
col <> CONVERT(col, 'AL32UTF8', 'AL32UTF8')

-- 4. Dupla codificação UTF-8 (mojibake: ç→Ã§, é→Ã©, NBSP→Â )
--    'SÃO'/'JOÃO' legítimos NÃO disparam (O é ASCII, fora de A0-BF)
REGEXP_LIKE(col, UNISTR('\00C3[\00A0-\00BF]|\00C2[\00A0-\00BF]'))

-- 5. Espaços nas pontas (resíduo CHAR→VARCHAR2)
col <> TRIM(col)

----------------------------------------------------------

CREATE TABLE audit_sujeira (
  table_name  VARCHAR2(128),
  column_name VARCHAR2(128),
  verificacao VARCHAR2(30),
  qtd         NUMBER,
  dt_execucao DATE DEFAULT SYSDATE
);

DECLARE
  v_fora_conjunto NUMBER; v_controle NUMBER; v_byte_invalido NUMBER;
  v_mojibake NUMBER; v_espacos NUMBER;
BEGIN
  FOR c IN (SELECT tc.table_name, tc.column_name
              FROM user_tab_columns tc
              JOIN user_tables t ON t.table_name = tc.table_name
             WHERE tc.data_type IN ('VARCHAR2','CHAR')
               AND tc.table_name NOT LIKE 'AUDIT\_%' ESCAPE '\'
             ORDER BY tc.table_name, tc.column_id) LOOP
    BEGIN
      EXECUTE IMMEDIATE
        'SELECT COUNT(CASE WHEN REGEXP_LIKE("'||c.column_name||'", UNISTR(''[^\0009\000A\000D\0020-\007E\00A7\00AA\00B0\00BA\00C0-\00FF]'')) THEN 1 END),
                COUNT(CASE WHEN REGEXP_LIKE("'||c.column_name||'", ''[[:cntrl:]]'') THEN 1 END),
                COUNT(CASE WHEN "'||c.column_name||'" <> CONVERT("'||c.column_name||'", ''AL32UTF8'', ''AL32UTF8'') THEN 1 END),
                COUNT(CASE WHEN REGEXP_LIKE("'||c.column_name||'", UNISTR(''\00C3[\00A0-\00BF]|\00C2[\00A0-\00BF]'')) THEN 1 END),
                COUNT(CASE WHEN "'||c.column_name||'" <> TRIM("'||c.column_name||'") THEN 1 END)
           FROM "'||c.table_name||'"'
      INTO v_fora_conjunto, v_controle, v_byte_invalido, v_mojibake, v_espacos;

      IF v_fora_conjunto > 0 THEN INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'FORA_CONJUNTO',v_fora_conjunto); END IF;
      IF v_controle      > 0 THEN INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'CONTROLE',v_controle); END IF;
      IF v_byte_invalido > 0 THEN INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'BYTE_INVALIDO_UTF8',v_byte_invalido); END IF;
      IF v_mojibake      > 0 THEN INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'DUPLA_CODIFICACAO',v_mojibake); END IF;
      IF v_espacos       > 0 THEN INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'ESPACOS_PONTAS',v_espacos); END IF;
    EXCEPTION WHEN OTHERS THEN
      INSERT INTO audit_sujeira (table_name,column_name,verificacao,qtd) VALUES (c.table_name,c.column_name,'ERRO: '||SUBSTR(SQLERRM,1,20),-1);
    END;
  END LOOP;
  COMMIT;
END;

-----------------------------------------------------------------

-- Diff de inventário canônico da coluna suspeita
SELECT origem, forma_canonica, COUNT(*) qtd
FROM (
    SELECT '19C'  AS origem, ASCIISTR(XPTO) AS forma_canonica FROM tabela@lnk19c
    UNION ALL
    SELECT '23AI' AS origem, ASCIISTR(XPTO) FROM tabela
)
GROUP BY origem, forma_canonica
ORDER BY forma_canonica, origem;
/

SELECT * FROM audit_sujeira ORDER BY qtd DESC;
