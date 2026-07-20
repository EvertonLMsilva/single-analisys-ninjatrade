# Validação do pullback v4 no MNQ

Data da validação: 2026-07-20.

## Objetivo

Confirmar se a versão `0.6.0-beta.1` e o setup experimental de pullback foram efetivamente aplicados ao MNQ, sem misturar os resultados com o MES ou com os formatos CSV anteriores.

## Fonte analisada

- diretório: `Documents/NinjaTrader 8/TradeAssistant/Data`;
- arquivos: 6 arquivos `*_MNQ_*_v4.csv`;
- datas dos sinais: 2026-07-14 a 2026-07-20;
- total: 205 registros;
- instrumento: `MNQ 09-26` em gráfico de 5 minutos.

Os arquivos v2 e v3 foram preservados, mas não participaram desta validação.

## Confirmações da aplicação

- todos os 205 registros usam a versão `0.6.0-beta.1`;
- 161 registros estão identificados como `TrendPullback`;
- 44 registros estão identificados como `EmaCrossBaseline`;
- tick registrado: `0,25`;
- valor do ponto registrado: `USD 2`;
- risco máximo por contrato: `USD 75`;
- política: `DescartarAcimaDoLimite`;
- os dois setups aparecem separados na coluna `Setup`.

Isso confirma que a versão nova foi processada para o MNQ e que a linha de base continuou sendo coletada em paralelo.

## Resultado inicial do TrendPullback

Dos 161 candidatos:

- 102 ficaram dentro do limite financeiro;
- 59 foram rejeitados por risco;
- 15 atingiram o alvo;
- 46 atingiram o stop;
- 39 expiraram;
- 2 foram ambíguos.

Entre os 61 resultados decididos, a taxa de alvos foi `24,59%`. Com o alvo configurado em aproximadamente 2R, o total registrado foi `-16R`. O risco dos candidatos variou de USD 8,50 a USD 263,50, com média de USD 70,57 e mediana de USD 63,50.

## Distribuição por data

| Data | Candidatos | Aceitos | Rejeitados | Alvos | Stops | Expirados | Ambíguos |
|---|---:|---:|---:|---:|---:|---:|---:|
| 2026-07-14 | 5 | 5 | 0 | 1 | 2 | 2 | 0 |
| 2026-07-15 | 43 | 35 | 8 | 2 | 15 | 16 | 2 |
| 2026-07-16 | 46 | 26 | 20 | 4 | 14 | 8 | 0 |
| 2026-07-17 | 28 | 13 | 15 | 5 | 6 | 2 | 0 |
| 2026-07-19 | 10 | 5 | 5 | 0 | 2 | 3 | 0 |
| 2026-07-20 | 29 | 18 | 11 | 3 | 7 | 8 | 0 |

## Conclusão

A implementação refletiu corretamente no MNQ. Não há evidência de falha de versão, separação de setup, identificação do instrumento ou aplicação do limite financeiro.

O desempenho inicial, porém, não aprova a regra: a amostra cobre somente seis datas processadas historicamente e apresenta resultado negativo. O próximo passo correto é validar visualmente alguns sinais no Playback e investigar horários, qualidade da tendência e validade de três candles antes de mudar qualquer parâmetro.
