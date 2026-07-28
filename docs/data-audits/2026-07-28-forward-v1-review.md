# Revisão da rodada prospectiva forward-2026-07-v1

## Escopo

Foram analisados os arquivos v6 e os resumos `validation_v1` de 23 a 28 de julho de 2026. O arquivo de 28/07 era parcial, com dados até aproximadamente 00:29. Os valores são hipotéticos e não incluem comissão, taxas ou slippage.

## Integridade

- 315 linhas foram encontradas nos CSVs brutos;
- os resumos atuais representam 310 sinais;
- cinco linhas antigas permaneceram nos CSVs MNQ após reprocessamento histórico;
- não foram encontradas chaves duplicadas, campos financeiros inconsistentes ou amostras fora da versão 0.8.0-beta.1;
- devido à diferença, os resumos atuais foram usados como fonte principal para o desempenho.

## Perfis principais

| Ativo e setup | Total | Decididos | Alvos | Stops | Expirados | Rejeitados | Acerto | Resultado R | Resultado USD |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| MES EmaCrossBaseline 1R | 33 | 9 | 4 | 5 | 23 | 1 | 44,44% | -1R | -52,50 |
| MNQ TrendPullback 1,5R | 115 | 63 | 26 | 37 | 19 | 31 | 41,27% | +2R | -105,25 |

O total financeiro dos perfis principais foi de -157,75 USD antes de custos.

No MES, 23 dos 32 sinais aceitos expiraram, cerca de 72%. A amostra decidida é pequena, mas o perfil atual produz poucas oportunidades avaliáveis.

No MNQ, o risco médio dos vencedores foi aproximadamente 35,37 USD e o dos perdedores 40,12 USD. Essa assimetria explica por que +2R coexistiu com resultado financeiro negativo. A maior sequência diária observada foi de sete stops e o maior drawdown diário foi 324,50 USD.

## Decisão

A rodada não aprova nenhum perfil para uso operacional. A configuração é preservada como referência e não será otimizada diretamente sobre os mesmos resultados.

A próxima etapa é diagnóstica:

1. corrigir a persistência para impedir registros órfãos;
2. medir direção, hora e faixa de risco em arquivos automáticos;
3. manter entradas, stops, alvos e limite financeiro inalterados durante a coleta diagnóstica;
4. propor uma única hipótese por ativo somente depois de obter arquivos íntegros.

