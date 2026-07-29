# Backtest offline de estratégias — MNQ e MES

Data da análise: 29/07/2026

## Decisão

Nenhuma nova regra foi aprovada para o indicador. Em especial, **não há evidência
suficiente para habilitar compras** em MNQ ou MES.

As regras que sobreviveram à seleção e à validação perderam dinheiro no período
final, que permaneceu fora da escolha dos parâmetros. Alterar o indicador com base
nelas aumentaria o risco de sobreajuste.

O `EvidencePullback` atualmente instalado também não é considerado aprovado para
execução automática. Ele permanece congelado apenas para acompanhamento prospectivo.

## Dados utilizados

| Ativo | Candles de 5 min | Sessões válidas | Seleção | Validação | Teste final |
|---|---:|---:|---:|---:|---:|
| MNQ | 11.576 | 40 | 24 | 8 | 8 |
| MES | 11.731 | 43 | 25 | 9 | 9 |

As sessões incompletas de MNQ em 08/07, 09/07 e 17/07 foram excluídas. A auditoria
de integridade e os hashes dos arquivos estão em
`docs/data-audits/2026-07-29-ohlcv-dataset-audit.md`.

## Método

- divisão cronológica de 60% para seleção, 20% para validação e 20% para teste final;
- o teste final não participou da escolha da regra;
- 3.888 configurações avaliadas por ativo e direção, totalizando 15.552;
- famílias avaliadas: pullback na EMA, retomada da VWAP e rompimento de 12 candles;
- filtros avaliados: score de contexto, inclinação/lado da VWAP, distância em ATR,
  volume relativo e horário;
- alvos de 1R e 1,5R, com validade de 3, 6 ou 12 candles;
- entrada presumida no fechamento do candle de sinal;
- acompanhamento iniciado apenas no candle seguinte;
- quando stop e alvo aparecem no mesmo candle, foi considerado o stop;
- risco técnico mínimo de USD 5 e máximo de USD 50 por contrato;
- custo conservador de USD 5 por operação completa;
- somente uma operação simultânea por configuração;
- intervalo mínimo de três candles entre gatilhos de pullback.

Para sobreviver antes do teste final, a configuração precisava apresentar:

- pelo menos 15 operações na seleção e 5 na validação;
- resultado positivo nos dois períodos;
- profit factor mínimo de 1,10 nos dois períodos.

## Resultado dos candidatos escolhidos sem olhar o teste final

| Ativo | Direção | Família | Seleção | Validação | Teste final | PF no teste | Decisão |
|---|---|---|---:|---:|---:|---:|---|
| MNQ | Compra | Rompimento | +USD 63,50 | +USD 112,00 | **-USD 230,25** | 0,223 | Reprovado |
| MNQ | Venda | Pullback | +USD 365,50 | +USD 132,75 | **-USD 132,25** | 0,507 | Reprovado |
| MES | Compra | — | — | — | — | — | Nenhuma regra sobreviveu |
| MES | Venda | Rompimento | +USD 167,50 | +USD 117,50 | **-USD 155,62** | 0,486 | Reprovado |

O candidato de compra do MNQ realizou 10 operações no teste: um alvo, seis stops e
três expirações. Nenhuma das seis sessões em que operou terminou positiva.

No MES comprado, nenhuma das 3.888 configurações atingiu simultaneamente os mínimos
de amostra, resultado e profit factor na seleção e na validação.

## Verificação da regra atualmente congelada

Foi reconstruído o `EvidencePullback` com os parâmetros documentados: MNQ vendido,
pullback, score mínimo 4/6, preço abaixo de VWAP descendente, alvo de 1R, validade de
três candles e risco máximo de USD 50.

| Período | Operações | Resultado líquido | PF | Drawdown |
|---|---:|---:|---:|---:|
| Seleção | 88 | -USD 77,00 | 0,942 | USD 397,50 |
| Validação | 50 | -USD 327,00 | 0,678 | USD 370,00 |
| Teste final | 44 | +USD 152,50 | 1,262 | USD 124,00 |
| Total | 182 | **-USD 251,50** | **0,914** | **USD 733,00** |

O trecho mais recente foi positivo, mas os períodos anteriores foram negativos. Isso
não confirma estabilidade. O resultado também não substitui os logs do indicador:
o backtest reconstrói sinais em todo o histórico exportado, usa candles OHLC de cinco
minutos e aplica um custo fixo, enquanto os logs existentes cobrem versões e janelas
de coleta diferentes.

## Limitações

- candles de cinco minutos não revelam a ordem intrabar entre stop e alvo;
- VWAP é aproximada com preço típico e volume do candle, não calculada tick a tick;
- o horário dos CSVs foi interpretado como horário local e a sessão foi virada às
  19:00;
- não foram modelados slippage variável, fila de execução ou atraso;
- 40–43 sessões ainda são uma amostra pequena diante de 15.552 configurações;
- este é um estudo de sinais, não uma promessa de resultado financeiro.

## Próximo passo recomendado

Não mudar as entradas do NinjaTrader com base nesta rodada. Para avançar sem esperar
novos dias, exportar MNQ e MES de cinco minutos com **o mesmo intervalo de pelo menos
seis meses; doze meses é preferível**. O próximo protocolo deve ser definido antes
de abrir o novo período final e testar:

1. estabilidade por mês e por regime de volatilidade;
2. confirmação cruzada entre MNQ e MES;
3. compras e vendas com regras congeladas;
4. validação walk-forward;
5. um período final completamente intocado.

O arquivo completo de resultados está em
`research/results/2026-07-29-offline-backtest.json` e o procedimento reproduzível em
`research/offline_backtest.py`.
