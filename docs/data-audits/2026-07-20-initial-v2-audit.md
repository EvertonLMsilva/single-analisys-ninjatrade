# Auditoria inicial dos CSVs v2 — 2026-07-20

## Objetivo

Verificar se os dados históricos disponíveis desde 2026-07-15 podem ser usados na análise inicial e confirmar a diferença de risco financeiro entre MNQ e MES.

## Escopo

- pasta analisada: `Documents/NinjaTrader 8/TradeAssistant/Data`;
- período dos candles: 2026-07-15 a 2026-07-20;
- gráfico: 5 minutos;
- instrumentos: `MNQ 09-26` e `MES 09-26`;
- versão do indicador: `0.4.0-beta.1`;
- formato incluído: somente arquivos `_v2.csv`;
- arquivos v1 foram preservados, mas excluídos desta comparação porque não possuem os campos financeiros atuais e foram gerados antes das correções mais recentes.

## Integridade

- 10 arquivos v2;
- 78 registros, sendo 39 de MNQ e 39 de MES;
- nenhuma chave vazia;
- nenhuma chave duplicada dentro dos arquivos;
- tick registrado para os dois ativos: `0.25`;
- valor do ponto registrado: MNQ `2`, MES `5`;
- limite máximo por contrato: não configurado em todos os 78 registros.

## Resumo

| Ativo | Sinais | Alvos | Stops | Expirados | Taxa entre decididos | Total R | Risco médio | Faixa de risco |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| MNQ 09-26 | 39 | 0 | 9 | 30 | 0,0% | -9 R | USD 110,14 | USD 58,50 a 204,00 |
| MES 09-26 | 39 | 3 | 4 | 32 | 42,9% | +2 R | USD 37,72 | USD 16,25 a 67,50 |

Expiraram 76,9% dos sinais de MNQ e 82,1% dos sinais de MES. No conjunto completo, 62 de 78 sinais expiraram.

## Interpretação inicial

Os dados confirmam que, com a configuração atual baseada em ATR, o risco financeiro do MNQ ficou substancialmente maior que o do MES. Isso ocorre pela distância do stop em pontos, mesmo o MNQ tendo menor valor monetário por ponto.

A quantidade elevada de sinais expirados indica que a validade de três candles, a distância dos níveis ou a própria regra demonstrativa precisam ser investigadas. Ainda não é correto escolher novos parâmetros apenas com esta amostra.

## Limitações

- são apenas cinco datas de negociação e 78 sinais;
- a regra de cruzamento de EMA é demonstrativa;
- sinais expirados possuem resultado de `0 R`, sem saída hipotética a mercado;
- os valores financeiros não incluem comissão, taxas ou slippage;
- os arquivos foram produzidos pelo processamento dos candles históricos carregados no gráfico, e não representam necessariamente observação manual em tempo real;
- não havia limite máximo de risco configurado.

## Decisão

Os arquivos v2 servem como linha de base preliminar e para validar a qualidade do registro. Eles não são suficientes para aprovar a estratégia.

Próximas ações:

1. configurar um limite de risco por contrato;
2. preservar esta amostra sem edições;
3. ampliar a coleta até pelo menos 20 pregões e 100 sinais por configuração relevante;
4. criar o relatório consolidado automático;
5. investigar os expirados antes de alterar vários parâmetros ao mesmo tempo.
