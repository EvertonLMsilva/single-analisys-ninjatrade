# Decisão 0018 - Adicionar observação universal em tempo real

Data: 2026-07-30

## Contexto

O candidato de momentum congelado produz no máximo uma análise perto do fechamento
e é válido apenas para MNQ. Isso não atende à necessidade de acompanhar possíveis
operações conforme o mercado evolui nem permite comparar novos ativos rapidamente.

## Decisão

Adicionar um modo universal, visual e experimental que avalia cada candle fechado
de 5 minutos entre 10:30 e 17:00. O sinal exige tendência e pullback contextual,
com EMA 9/21, VWAP de sessão, ATR, força do candle e volume relativo. O NinjaTrader
fornece tick, valor do ponto e moeda usados nos níveis e no risco financeiro.

Somente um sinal universal pode permanecer ativo por gráfico. O candidato de
momentum MNQ continua disponível, mas fica desligado por padrão para evitar
sobreposição. Nenhuma chamada de ordem é adicionada.

## Limites

O modo não foi validado para todos os instrumentos e não deve ser tratado como
recomendação lucrativa. A avaliação ocorre no fechamento do candle, não a cada
tick. O horário usa o fuso configurado no NinjaTrader. Os resultados são gravados
separadamente em `TradeAssistant/Realtime` para permitir comparação futura sem
misturar as rodadas antigas.

## Ajuste visual 1.3.1

O modo universal preserva todos os desenhos correspondentes aos dias carregados,
independentemente das preferências antigas de limite visual salvas no workspace.
O painel identifica apenas o `PULLBACK CONTEXTUAL` e não declara nenhum ativo
aprovado. As duas amostras universais disponíveis ainda não demonstraram vantagem:
MNQ tinha 19 sinais e -3R; MES tinha cinco sinais e 0R.
