# Fase 2 — Deep Research (Obsolescência e Regressões)

Você é o agente de pesquisa técnica. **Esta fase exige busca web — troque de
modelo se o atual não tiver essa capacidade.**

## Entrada
- `01-inventario.md`
- `config/catalogo-obsolescencia.md` (já vem populado com achados confirmados;
  esta fase expande, não substitui)

## Instruções

1. Para cada chamada a package built-in listada em `01-inventario.md`, verifique
   contra `config/catalogo-obsolescencia.md`. Se **não estiver catalogada**,
   pesquise:
   - "{package} deprecated OR desupported Oracle 23ai 26ai"
   - Documentação oficial: `docs.oracle.com/en/database/oracle/oracle-database/26/`
     (Changes in This Release, Deprecated and Desupported Features)
   - Se removido/deprecated: qual o substituto recomendado pela própria
     documentação Oracle

2. Para os comandos de risco levantados na Fase 1 (UNION, UNION ALL, MERGE,
   hints, `CONNECT BY`, joins `(+)`), pesquise regressões de otimizador
   conhecidas entre a versão de origem e a versão alvo — não assuma que
   sintaxe válida = comportamento idêntico. Use termos como "Oracle {origem}
   to {alvo} optimizer regression UNION" ou busque no My Oracle Support
   (referencie se encontrar Doc ID relevante, mesmo sem acesso ao conteúdo
   completo).

3. Pesquise mudanças de comportamento de CRUD relevantes ao schema real:
   NLS/collation, tratamento de NULL em agregações, comportamento de
   `MERGE` com múltiplos matches, mudança de default de parâmetros de
   otimizador entre as versões.

4. **Atualize `config/catalogo-obsolescencia.md`** acrescentando linhas novas
   nas tabelas existentes (não crie um arquivo separado — o catálogo é
   cumulativo entre sessões).

5. Gere `02-obsolescencia.md` na pasta da sessão, específico deste inventário:
   - Tabela: Objeto (onde usa) | Item obsoleto/de risco | Status | Substituto
     | Ação necessária
   - Seção de comandos de risco de regressão silenciosa, cruzada com os
     objetos do inventário que os usam

## Saída esperada
- `02-obsolescencia.md` na sessão
- `config/catalogo-obsolescencia.md` atualizado (efeito colateral esperado e
  desejado — enriquece sessões futuras)
