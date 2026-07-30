# Auditoria — busca de gatilhos e modelo de eventos

Data: 2026-07-29.

## Objetivo

Continuar os ajustes da pesquisa por regime e estrutura até encontrar o melhor cenário
defensável nos dados disponíveis, sem promover uma regra apenas por lucro dentro da
amostra.

O indicador e a instalação do NinjaTrader permaneceram inalterados.

## Pesquisa manual de gatilhos

Foram comparados 12 mecanismos em MNQ e MES, com alvos de 1R, 1,5R e 2R:

- aceitação do rompimento;
- reteste confirmado;
- recuperação e sustentação da VWAP;
- recuperação da faixa de abertura;
- continuação e pullback de momentum;
- varredura em equilíbrio e recuperação da VWAP;
- versões independentes de regime com concordância MNQ/MES.

Isso produziu 36 candidatos por ativo. Cada candidato foi dividido em:

- seleção: 53 pregões;
- validação: 27 pregões;
- confirmação: 27 pregões.

Um candidato individual passou na seleção e nenhum passou na validação. Treze
componentes positivos e explicáveis na seleção formaram 71 combinações de no máximo
dois playbooks, sempre com uma operação por dia. Nenhuma combinação passou seleção e
validação.

O diagnóstico MFE/MAE mostrou que vários eventos alcançam 1R favorável, mas também
sofrem excursão adversa superior a 1R. O primeiro ponto de entrada não separa
continuação de falha com estabilidade. No MNQ, grande parte dos eventos posteriores
também excede o teto de USD 75 mesmo com apenas um micro.

## Modelo de eventos

Como verificação independente, foi criado um classificador logístico com:

- amostragem a cada cinco minutos;
- no máximo uma operação por ativo e dia;
- 22 variáveis disponíveis no instante do evento;
- compras e vendas;
- alvos de 1R, 1,5R e 2R;
- limiar escolhido somente dentro do bloco inicial;
- versão congelada e versão reestimada a cada cinco pregões usando os 40 anteriores.

Foram avaliados 12 modelos finais. Somente um passou pela validação:

| Cenário | Validação | Confirmação |
|---|---:|---:|
| MNQ, 2R, modelo congelado | 17 operações, +USD 108,50, PF 1,325 | 16 operações, -USD 145,50, PF 0,646 |

A confirmação teve 31,25% de acerto, 11 stops em 16 operações e drawdown de
USD 242,50 por micro. A adaptação móvel de 40 pregões não melhorou a estabilidade:
nenhum modelo adaptativo passou a validação.

## Portão econômico

Para o melhor cenário observado, quantidades de 1 a 30 micros foram aplicadas a todas
as janelas de 20 pregões fora da seleção.

O melhor resultado ocorreu com oito micros:

- aprovação: 28,57%;
- falha por drawdown: 14,29%;
- drawdown P90: USD 1.684;
- saldo mediano ao final: -USD 196;
- melhor saldo de janela: +USD 1.756;
- pior saldo de janela: -USD 1.684.

Ele não atingiu o mínimo de 60% de aprovação e ultrapassou o teto de USD 1.000 para o
drawdown P90.

## Conclusão

O melhor cenário encontrado é MNQ, alvo 2R e modelo congelado, mas ele está
**reprovado**. O ganho de validação não se repetiu no período seguinte. Aumentar
contratos melhora algumas janelas, porém não cria vantagem e eleva o drawdown.

Continuar escolhendo filtros nos mesmos 107 pregões seria otimização retrospectiva.
O próximo avanço confiável exige:

1. dados posteriores a 29/07, mantidos sem consulta durante a coleta;
2. validação prospectiva do melhor cenário apenas como referência silenciosa;
3. histórico bid/ask ou volumétrico antes de incorporar delta, imbalance ou absorção;
4. nova seleção somente após acumular um bloco adicional previamente definido.

## Artefatos

- `research/regime_trigger_walkforward.py`;
- `research/event_model_walkforward.py`;
- `research/results/2026-07-29-regime-trigger-walkforward.json`;
- `research/results/2026-07-29-event-model-walkforward.json`.
