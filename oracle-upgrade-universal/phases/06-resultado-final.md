# Fase 6 — Resultado Final

## Entrada
- Todos os `tasks/*.result.md` da sessão

## Instruções

1. Consolide status por wave e por tipo de validação (compilação /
   equivalência / substituição).
2. Destaque separadamente:
   - Objetos bloqueados por divergência de resultado (risco de dado errado
     em produção se ignorado)
   - Objetos bloqueados aguardando SR Oracle (risco de cronograma)
3. Gere `04-resultado-final.md` com:
   - Resumo executivo (nº objetos migrados, nº de substituições obrigatórias
     feitas, nº de divergências de resultado encontradas)
   - Tabela: Objeto | Tipo de validação | Status | Observação
   - Lista de SRs Oracle abertos (se houver) com status
   - Riscos residuais e recomendação (ex: rodar em paralelo 19c+26ai por N
     dias antes de decommission, monitorar objetos com equivalência
     "aceita com ressalva")
4. Atualize `INDEX.md` da raiz, marcando sessão como `concluída`.

## Saída esperada
`04-resultado-final.md` + `INDEX.md` atualizado.
