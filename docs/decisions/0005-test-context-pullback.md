# 0005 — Testar pullback com contexto de mercado

Data: 2026-07-28

## Situação

A auditoria dos 504 registros v7 mostrou que as compras do MNQ pullback produziram -12R e -USD 859,50 em 1R, enquanto as vendas produziram +16R e +USD 528,00. Isso não demonstra que o mercado deva ser operado apenas vendido. Demonstra que a regra anterior reconhecia tendência somente por posição e inclinação de um candle das EMAs.

A entrada anterior não conhecia VWAP, inclinação acumulada, extensão do preço, força do candle, volume relativo ou estrutura da sessão.

## Decisão

Criar o setup experimental `ContextPullback` com:

- mesma detecção inicial de pullback na EMA rápida;
- preço do lado direcional da VWAP da sessão;
- VWAP inclinada na direção do sinal;
- inclinação das EMAs medida em três candles e normalizada por ATR;
- corpo mínimo de 0,15 ATR;
- fechamento na parte superior de 35% do candle para compra ou inferior de 35% para venda;
- distância máxima de 1,25 ATR até a VWAP;
- volume relativo de pelo menos 0,8 como ponto do score;
- score mínimo de 5 em 6;
- risco máximo de USD 50;
- alvo de validação em 1R.

Lado da VWAP, inclinação da VWAP, EMAs, candle e extensão são obrigatórios. O volume pode faltar e ainda permitir score 5.

## Medição

- rodada: `context-2026-07-v3`;
- início prospectivo: 29/07/2026;
- CSV bruto: v8;
- resumo: `validation_v3`;
- segmentos: `segments_v2`;
- mínimo: cinco sessões completas e 30 resultados decididos;
- compras e vendas serão avaliadas separadamente;
- o `TrendPullback` anterior permanece silencioso como referência.

## Limitações

A VWAP é aproximada com preço típico e volume de cada candle de cinco minutos. Ela não substitui cálculo tick a tick. Máxima e mínima da sessão são registradas para análise, mas ainda não bloqueiam entradas. Nenhuma execução de ordens foi adicionada.
