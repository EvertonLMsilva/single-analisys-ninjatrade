# Auditoria dos dados OHLCV — MNQ e MES

## Objetivo

Validar os arquivos de candles de cinco minutos antes de utilizá-los para pesquisa e
backtest offline de estratégias compradoras e vendedoras.

## Arquivos

### MNQ

- arquivo: `NT_Optimizer_Data_MNQ_09-26_5Min_58d.csv`;
- período: 31/05/2026 19:05 até 29/07/2026 14:20;
- linhas: 11.576;
- datas distintas: 52;
- sessões estimadas: 44;
- SHA-256: `BEFF405F0A2F94599E3CDF171E78900C983811E3BEC52908B8432F5D5EB458CE`.

### MES

- arquivo: `NT_Optimizer_Data_MES_09-26_5Min_58d.csv`;
- período: 31/05/2026 19:05 até 29/07/2026 14:35;
- linhas: 11.731;
- datas distintas: 52;
- sessões estimadas: 43;
- SHA-256: `1C91C396CA3616EB6555D091E54EB303BCA6290F259859F7AD47C1128408F5FF`.

## Integridade individual

Os dois arquivos possuem:

- cabeçalho `Time,Open,High,Low,Close,Volume`;
- timestamps em ordem crescente;
- nenhuma duplicação;
- nenhuma inconsistência entre abertura, máxima, mínima e fechamento;
- nenhum preço incompatível com o tick de 0,25;
- nenhum volume negativo ou zerado.

## Alinhamento

- timestamps comuns: 11.576;
- cobertura conjunta: 98,6787%;
- nenhum timestamp existe apenas no MNQ;
- 155 timestamps existem apenas no MES.

Os 155 candles adicionais do MES estão em quatro blocos:

1. 08/07 11:25 até 08/07 18:00 — 80 candles;
2. 08/07 19:05 até 09/07 00:00 — 60 candles;
3. 17/07 12:00 até 17/07 12:55 — 12 candles;
4. 29/07 14:25 até 29/07 14:35 — três candles exportados depois do MNQ.

## Decisão de uso

- cálculos de cada ativo podem usar seu próprio arquivo;
- comparações sincronizadas entre MES e MNQ usarão somente timestamps comuns;
- sessões do MNQ afetadas pelos blocos de 08–09/07 e 17/07 serão excluídas de avaliações
  que dependam de contexto contínuo de sessão;
- os três candles finais adicionais do MES não representam falha do MNQ, apenas diferença
  no horário de exportação;
- os arquivos permanecerão fora do repositório; os hashes identificam exatamente a amostra;
- qualquer nova exportação deverá receber nova auditoria e novo hash.

## Limitações

- os timestamps não informam explicitamente o fuso horário;
- os arquivos não possuem bid, ask ou sequência intrabar;
- alvo e stop tocados no mesmo candle devem ser tratados como ambíguos ou pelo pior caso;
- comissão e slippage deverão ser premissas explícitas do backtest.

