# Plano provisório de risco — Take Profit Trader 25k

Data da análise: 2026-07-20.

## Informações fornecidas pelo operador

- mesa: Take Profit Trader;
- conta: USD 25.000;
- drawdown máximo informado: USD 1.500;
- orçamento máximo pessoal de perda por dia: USD 300;
- risco por operação: ainda não definido.

## Regras externas verificadas

Na avaliação de 25k, a Take Profit Trader informa drawdown trailing de fim de dia de USD 1.500, profit target de USD 1.500 e máximo de 3 contratos ou 30 micros.

Na conta PRO, o trailing é intradiário e acompanha o pico do saldo, incluindo ganhos não realizados. O limite para de subir quando alcança o saldo inicial. Atingir o saldo mínimo pode liquidar a conta imediatamente.

A comunicação atual da empresa informa remoção do limite diário obrigatório. O operador confirmou que os USD 300 são seu orçamento máximo pessoal de perda por dia, e não uma regra da mesa.

Referências:

- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15170265979165-Rule-3-Do-Not-Hit-End-Of-Day-EOD-Maximum-Trailing-Drawdown
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15171769361053-PRO-Account-Rules
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15169066911133-Rule-2-Do-Not-Exceed-Maximum-Position-Size

## Cenários considerados

| Risco por operação | % do drawdown | % do limite diário | Três stops | MNQ aceitos | MES aceitos |
|---:|---:|---:|---:|---:|---:|
| USD 50 | 3,3% | 16,7% | USD 150 | 0 de 40 | 31 de 40 |
| USD 60 | 4,0% | 20,0% | USD 180 | 1 de 40 | 36 de 40 |
| USD 75 | 5,0% | 25,0% | USD 225 | 7 de 40 | 40 de 40 |
| USD 90 | 6,0% | 30,0% | USD 270 | 15 de 40 | 40 de 40 |
| USD 100 | 6,7% | 33,3% | USD 300 | 20 de 40 | 40 de 40 |

## Recomendação inicial

- risco máximo técnico por operação: USD 75;
- quantidade inicial: 1 microcontrato;
- parada operacional diária: USD 225 ou 3 stops completos, o que acontecer primeiro;
- teto pessoal absoluto: USD 300;
- margem entre USD 225 e USD 300: USD 75 para comissão, taxas, slippage e variações de execução; essa margem não deve ser usada para iniciar uma nova operação;
- sinal acima de USD 75: descartar, sem aproximar artificialmente o stop;
- sinal descartado: registrar no CSV como `RiskRejected`;
- não usar o máximo de 30 micros permitido pela mesa como referência de tamanho de posição.

O limite de USD 75 é provisório e não garante cumprimento do trailing, especialmente em conta PRO. O indicador não acessa saldo, pico intradiário, drawdown restante ou ordens reais.

## Impacto esperado na amostra atual

- MNQ: 7 de 40 sinais seriam elegíveis;
- MES: 40 de 40 sinais seriam elegíveis;
- o exemplo de aproximadamente USD 154 de risco no MNQ seria descartado;
- o filtro favorece o ativo que oferece stop técnico compatível com o orçamento financeiro.

## Próximas decisões

1. confirmar se a conta é avaliação, PRO ou PRO+;
2. implementar os modos `Somente avisar` e `Descartar acima do limite`;
3. validar o filtro sobre os mesmos candles históricos;
4. somente depois comparar alvos de 1R, 1,5R e 2R.
