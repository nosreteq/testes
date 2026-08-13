# Fase 3 — Plano de Validação

## Entrada
- `01-inventario.md`
- `02-obsolescencia.md`

## Instruções

1. Use o grafo de dependência da Fase 1 para ordenar a validação: objetos
   sem dependência de outros objetos do escopo primeiro (ex: tabelas base,
   packages utilitários), depois o que depende deles (views, procedures que
   os chamam), depois a camada de aplicação (CRUD).

2. Para cada objeto/grupo, classifique o tipo de validação necessária:
   - **Compilação**: package/procedure/function/trigger só precisa compilar
     limpo na versão alvo (pega erro de sintaxe/API removida)
   - **Equivalência de resultado**: view, comando CRUD ou procedure com
     lógica de agregação/junção precisa ter o *resultado* comparado entre
     19c e 26ai para os mesmos dados de entrada — não basta compilar
   - **Substituição obrigatória**: objeto usa item marcado como "Removido" no
     catálogo — não vai compilar, precisa reescrita antes de qualquer teste

3. Priorize por risco: objetos que usam `UNION`/`UNION ALL`/`MERGE` ou
   packages com breaking change confirmado entram como **equivalência de
   resultado**, mesmo que pareçam simples.

4. Gere `03-plano-validacao.md` com:
   - Waves de validação, ordenadas por dependência
   - Tipo de validação por objeto/grupo (compilação / equivalência / substituição)
   - Critério de aceite por wave

## Saída esperada
`03-plano-validacao.md`, pronto para virar tasks na Fase 4.
