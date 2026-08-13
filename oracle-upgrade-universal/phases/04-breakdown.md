# Fase 4 — Breakdown por Objeto/Grupo

## Entrada
- `03-plano-validacao.md`
- `model-selection.md`

## Instruções

1. Para cada objeto/grupo do plano, crie `tasks/<objeto_ou_grupo>.task.md`
   contendo:
   - Tipo de validação exigido (compilação / equivalência de resultado /
     substituição obrigatória)
   - Código atual (fonte PL/SQL, ou definição da view/comando CRUD)
   - Itens do catálogo de obsolescência que se aplicam a este objeto
   - Para equivalência de resultado: como obter dado de teste (query de
     amostra, ou indicação de rodar contra staging com dado real)
   - Critério de "done" específico por tipo:
     - Compilação: `ALL_OBJECTS.STATUS = 'VALID'` na versão alvo
     - Equivalência: mesmo resultset (linhas e valores) entre 19c e 26ai
       para a mesma entrada, ou divergência documentada e justificada
     - Substituição: código reescrito, compilado, e testes de
       compilação/equivalência aplicados ao código novo

2. Agrupe objetos pequenos e relacionados (ex: várias functions utilitárias
   do mesmo package) numa task só, se não fizer sentido paralelizar em
   excesso — o paralelismo da Fase 5 vale a pena por objeto complexo, não
   por função trivial.

## Saída esperada
Um `.task.md` por objeto/grupo, referenciado a partir de `03-plano-validacao.md`.
