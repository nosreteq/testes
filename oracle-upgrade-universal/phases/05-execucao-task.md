# Fase 5 — Execução (Validação e Correção de Objeto)

**Cole este prompt uma vez por task. Pode rodar em paralelo, uma instância por
objeto/grupo — o prompt não muda, só o `.task.md` de entrada.**

## Entrada
- `tasks/<objeto_ou_grupo>.task.md`

## Instruções por tipo de validação

### Se "Substituição obrigatória"
1. Reescreva o código usando o substituto indicado no catálogo de
   obsolescência (ex: `DBMS_OBFUSCATION_TOOLKIT.MD5` → `DBMS_CRYPTO.HASH`
   com constante `HASH_SH256` — **não use MD4/MD5 no código novo**, mesmo que
   o objeto original usasse, a menos que haja exigência externa de
   compatibilidade de hash já armazenado).
2. Se a mudança de algoritmo/API alterar o *formato* do dado gerado (ex:
   hash MD5 de 16 bytes vs SHA-256 de 32 bytes), sinalize isso explicitamente
   — pode exigir migração de dado já persistido, não só troca de código.
3. Compile. Se limpo, trate como "Compilação" a partir daqui.

### Se "Compilação"
1. Compile o objeto na versão alvo (ou valide sintaxe estaticamente contra
   as mudanças do catálogo, se não houver acesso a instância 26ai real).
2. Corrija qualquer erro de compilação decorrente de API removida/alterada.

### Se "Equivalência de Resultado" — **a validação mais importante deste plugin**
1. Execute o comando/query original contra a base 19c com um conjunto de
   dados de teste (amostra representativa, não só caso trivial — inclua
   casos de borda: NULL, duplicatas, conjunto vazio).
2. Execute o mesmo comando contra 23ai/26ai, mesmo dado de teste.
3. Compare linha a linha. Use o template `templates/regression-test.sql.tpl`
   como estrutura de script de comparação.
4. Se os resultados divergirem:
   - Confirme que não é diferença esperada (ex: ordenação sem `ORDER BY`
     explícito nunca foi garantida)
   - Se for regressão real (dado faltando, resultado incorreto), documente
     como candidato a SR Oracle + SQL Patch — **não tente "consertar" a
     query mudando a lógica de negócio só para bater com o resultado novo**,
     a menos que o resultado novo esteja correto e o antigo é que estava
     errado (validar com o dono do dado/regra de negócio)
5. Capture o plano de execução (`EXPLAIN PLAN` / `DBMS_XPLAN`) das duas
   versões se houver divergência — ajuda a instruir o SR.

## Saída esperada
`tasks/<objeto_ou_grupo>.result.md` com:
- Status: `sucesso` / `sucesso com ressalvas` / `bloqueado — divergência de
  resultado` / `bloqueado — aguardando SR Oracle`
- Evidência da validação (resultado da comparação, não só "testei")
- Se aplicável, script de correção ou nota do SR aberto
