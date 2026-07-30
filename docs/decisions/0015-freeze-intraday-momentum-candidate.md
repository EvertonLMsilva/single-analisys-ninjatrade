# Decisão 0015 — Congelar candidato de momentum intradiário

Data: 2026-07-29.

## Contexto

As análises anteriores de pullback, regime, estrutura e modelo de eventos não
produziram estabilidade. Foi iniciada uma pesquisa baseada em momentum intradiário,
overnight, gap, abertura e força relativa.

## Decisão

- congelar o candidato de momentum intradiário em MNQ;
- usar volatilidade dos primeiros 30 minutos para escolher entre o sinal simples da
  primeira meia hora e a soma da primeira com a penúltima meia hora;
- entrar somente na última meia hora e sair no fechamento;
- manter stop de USD 75 por micro;
- iniciar a validação futura com apenas um micro hipotético;
- não usar ainda o dimensionamento de 2/4 micros;
- não alterar a regra durante os próximos 20 pregões;
- não chamar o candidato de validado antes de cumprir o protocolo prospectivo.

## Evidência

No período de desenvolvimento, o candidato realizou 81 operações, ganhou USD 1.244
por micro, apresentou PF 1,416, drawdown de USD 621 e operou todos os pregões. Quatro
dos cinco meses foram positivos.

O melhor dimensionamento alcançou 50% de aprovação histórica, abaixo do mínimo de
60%, e drawdown P90 de USD 1.592,80. Por isso, somente a vantagem com um micro será
testada inicialmente.

Detalhes:
`docs/data-audits/2026-07-29-alternative-market-methods.md`.

## Consequência

A pesquisa finalmente possui uma regra historicamente lucrativa e explicável para
congelar. Sua validade dependerá integralmente dos dados posteriores a 29/07.
