# Auditoria dos CSVs v2 regenerados — 2026-07-20

## Objetivo

Validar os arquivos recriados após a remoção dos logs anteriores e a nova carga dos indicadores MNQ e MES.

## Escopo

- 10 arquivos `_v2.csv`;
- candles entre 2026-07-15 e 2026-07-20;
- `MNQ 09-26` e `MES 09-26` em gráfico de 5 minutos;
- versão `0.4.0-beta.1`;
- 80 registros, sendo 40 por ativo.

## Integridade

- nenhuma chave vazia;
- nenhuma chave duplicada dentro dos arquivos;
- nenhum valor numérico inválido;
- nenhum erro no cálculo de risco financeiro, alvo financeiro ou quantidade de ticks;
- nenhum preço fora do múltiplo do tick;
- nenhuma inversão entre entrada, stop e alvo;
- todos os sinais encerrados possuem horário de encerramento;
- os dois sinais ativos não possuem horário de encerramento, como esperado;
- tick `0.25`, valor do ponto `2` para MNQ e `5` para MES;
- limite máximo por contrato permanece não configurado.

## Resumo

| Ativo | Sinais | Alvos | Stops | Expirados | Ativos | Taxa entre decididos | Total R | Risco médio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| MNQ 09-26 | 40 | 0 | 9 | 30 | 1 | 0,0% | -9 R | USD 111,24 |
| MES 09-26 | 40 | 3 | 4 | 32 | 1 | 42,9% | +2 R | USD 38,38 |

Entre os sinais já encerrados, expiraram 76,9% no MNQ e 82,1% no MES.

## Sinais ativos no momento da auditoria

| Ativo | Horário | Direção | Entrada | Stop | Alvo | Risco | Alvo financeiro |
|---|---|---|---:|---:|---:|---:|---:|
| MES 09-26 | 2026-07-20 13:55 | Venda | 7513,75 | 7526,50 | 7488,25 | USD 63,75 | USD 127,50 |
| MNQ 09-26 | 2026-07-20 14:00 | Venda | 28983,00 | 29060,00 | 28829,00 | USD 154,00 | USD 308,00 |

Os sinais ativos ainda não devem ser contabilizados como ganho, perda ou expirado.

## Comparação com a auditoria anterior

- os 78 sinais já encerrados mantiveram os mesmos totais por resultado;
- foram adicionados somente os dois sinais ativos do último candle disponível;
- a regeneração produziu arquivos consistentes e sem duplicações;
- os arquivos v1 deixaram de estar presentes, permanecendo apenas o formato atual v2.

## Decisão

Os logs regenerados são íntegros e servem como linha de base preliminar. A estratégia ainda não pode ser considerada validada devido à pequena quantidade de datas, à regra demonstrativa e ao grande número de expirados.

Próximas ações:

1. definir o limite máximo de risco por contrato;
2. aguardar ou reproduzir o encerramento dos dois sinais ativos;
3. consolidar automaticamente os resultados por dia e ativo;
4. ampliar a amostra sem modificar a configuração durante a rodada.
