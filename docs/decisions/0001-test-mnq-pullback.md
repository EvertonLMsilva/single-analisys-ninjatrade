# Decisão 0001 — Testar pullback no MNQ

Data: 2026-07-20.

Situação: aprovado para experimento, não aprovado para operação real.

## Contexto

A regra demonstrativa atual exige cruzamento de EMA e stop de `1,5 × ATR`. Com limite de USD 75 por microcontrato no MNQ:

- 44 oportunidades foram detectadas;
- 37 foram descartadas por risco;
- 7 foram aceitas;
- as aceitas produziram 5 expirações e 2 stops;
- nenhum alvo foi atingido.

Tendências que já começaram também não geram nova entrada sem outro cruzamento.

## Decisão

Criar um setup experimental de pullback a favor da tendência para buscar entrada posterior ao cruzamento e stop técnico compatível com o orçamento.

O experimento não substituirá imediatamente a regra atual. Os dois setups deverão ser identificados separadamente no CSV e comparados sobre os mesmos dados.

## Restrições

- manter risco máximo de USD 75 por microcontrato;
- manter quantidade de referência em 1 micro;
- não aproximar artificialmente o stop para fazer o sinal passar;
- registrar sinais descartados;
- não executar ordens;
- alterar uma variável principal por rodada;
- manter MNQ e MES separados nos relatórios.

## Primeira hipótese a testar

- direção definida pelas EMAs rápida e lenta;
- aguardar retorno do preço em direção à região das médias;
- exigir candle de confirmação na direção da tendência;
- usar o extremo técnico do pullback como candidato a stop;
- rejeitar quando o risco desse stop ultrapassar USD 75;
- acompanhar alvos de 1R, 1,5R e 2R em modo comparativo, sem escolher antecipadamente o vencedor.

Os detalhes exatos de distância até as médias e candle de confirmação devem ser configuráveis e documentados na versão experimental.

## Critérios de avaliação

- pelo menos 20 pregões e 100 oportunidades detectadas antes de uma decisão final;
- comparar quantidade aceita, rejeitada e expirada;
- comparar resultado em R e taxa entre resultados decididos;
- medir MFE e MAE;
- verificar concentração por horário;
- incluir custos estimados antes de qualquer conclusão operacional;
- validar no Playback além do processamento histórico do gráfico.

## Resultado esperado desta etapa

O objetivo não é aumentar o número de operações a qualquer custo. O objetivo é descobrir se existe uma entrada tecnicamente justificável no MNQ que respeite o orçamento financeiro da conta Test 25k.
