# Auditoria — regime e estrutura em barras de 1 minuto

Data: 2026-07-29.

## Objetivo

Testar uma abordagem diferente das combinações anteriores de indicadores:

1. classificar o estado da abertura;
2. escolher um playbook compatível com esse estado;
3. exigir concordância direcional entre MNQ e MES;
4. aplicar um gatilho estrutural;
5. validar o resultado econômico em janelas de 20 pregões.

O estudo é somente offline. O indicador, a instalação do NinjaTrader e a execução de
ordens não foram alterados.

## Dados

| Ativo | Linhas | Primeiro registro | Último registro | Intervalos de 1 min | SHA-256 |
|---|---:|---|---|---:|---|
| MNQ 09-26 | 146.890 | 01/03/2026 20:01 | 29/07/2026 16:50 | 99,9265% | `35B48A3DC10225A372564414FA9E7075951F08295B20EF76B10CFB3044307163` |
| MES 09-26 | 147.785 | 01/03/2026 20:01 | 29/07/2026 16:51 | 99,9269% | `A8D6E3898980407223E459925A4BA566017305AD9347BBE694E1425EE1F4EA66` |

Foram obtidos 107 pregões compartilhados com abertura regular suficiente. Os cinco
primeiros candles de um minuto do MNQ recompõem exatamente o primeiro candle de
cinco minutos exportado anteriormente, confirmando a granularidade.

## Método congelado da primeira hipótese

O regime é determinado 30 minutos depois da abertura regular. Somente dados
anteriores ou iguais a esse horário são usados:

- retorno e eficiência direcional dos primeiros 30 minutos;
- faixa de abertura relativa às 20 sessões anteriores;
- VWAP, inclinação da VWAP e aceitação dos últimos cinco candles;
- volume de abertura relativo às 20 sessões anteriores;
- direção e aceitação do outro índice.

Estados possíveis: `TrendUp`, `TrendDown`, `Balance` e `Transition`.

Playbooks:

- `TrendRetest`: rompimento da faixa de abertura e reteste a favor da tendência;
- `BalanceRejection`: falso rompimento da faixa em dia de equilíbrio, com alvo na
  VWAP;
- nenhuma entrada em `Transition`.

Foram mantidos custo de USD 5 por operação e risco entre USD 10 e USD 75 por
contrato. Caso stop e alvo apareçam no mesmo candle, o resultado é tratado como
perda.

## Resultado

| Ativo | Operações | Alvos | Stops | Líquido | PF | Drawdown |
|---|---:|---:|---:|---:|---:|---:|
| MNQ | 30 | 4 | 26 | -USD 846,76 | 0,355 | USD 1.008,44 |
| MES | 6 | 4 | 2 | +USD 36,02 | 1,963 | USD 24,78 |

No MNQ, os três blocos cronológicos foram negativos: -USD 585,28, -USD 23,16 e
-USD 238,32. Compras e vendas também foram negativas. Portanto, não existe um
subgrupo estável que justifique promover o gatilho.

No MES, a amostra de seis operações é insuficiente. O bloco intermediário teve
somente uma operação e ela foi perdedora. O lucro total não demonstra repetibilidade
nem frequência suficiente.

## Portão econômico

Foram simuladas todas as janelas consecutivas de 20 pregões e quantidades de 1 a 30
micros, com meta de USD 1.500, drawdown trailing de USD 1.500, mínimo de cinco dias
ativos, melhor dia abaixo de 50% do lucro, mínimo de 60% de aprovação, no máximo 15%
de quebra e drawdown P90 de até USD 1.000.

Nenhuma quantidade foi aprovada para MNQ ou MES.

## Interpretação

A classificação de contexto reduziu a busca a hipóteses explicáveis, mas não
transformou um reteste simples em vantagem. O problema principal desta versão está no
gatilho: no MNQ, o primeiro reteste após o rompimento frequentemente continua até o
stop. No MES, o teto financeiro descarta muitas estruturas e a frequência restante é
baixa.

Não será feita uma busca ampla de limiares sobre os mesmos 107 pregões. O próximo
experimento deve medir MFE/MAE dos eventos e testar um gatilho qualitativamente
diferente — aceitação após reteste ou recuperação de nível — em walk-forward. Dados
bid/ask serão necessários antes de afirmar que delta, imbalance ou absorção confirmam
uma entrada; OHLCV não contém essas informações.

## Artefatos

- código: `research/regime_structure_backtest.py`;
- resultado: `research/results/2026-07-29-regime-structure-backtest.json`;
- decisão: `docs/decisions/0013-reject-first-regime-structure-trigger.md`.
