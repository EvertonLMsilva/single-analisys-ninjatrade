# Plano provisório de risco — Take Profit Trader 25k

Data da análise: 2026-07-20.

## Informações fornecidas pelo operador

- mesa: Take Profit Trader;
- conta: USD 25.000;
- drawdown máximo informado: USD 1.500;
- limite pessoal de perda diária: ainda não definido;
- risco por operação: ainda não definido.
- requisito econômico do projeto: atingir USD 1.500 em no máximo 20 pregões.

## Regras externas verificadas

Na avaliação de 25k, a Take Profit Trader informa drawdown trailing de fim de dia de USD 1.500, profit target de USD 1.500 e máximo de 3 contratos ou 30 micros.

Na conta PRO, o trailing é intradiário e acompanha o pico do saldo, incluindo ganhos não realizados. O limite para de subir quando alcança o saldo inicial. Atingir o saldo mínimo pode liquidar a conta imediatamente.

A comunicação atual da empresa informa remoção do limite diário obrigatório. A menção
anterior a USD 300 foi um mal-entendido e não deve ser tratada como limite escolhido
pelo operador.

Referências:

- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15170265979165-Rule-3-Do-Not-Hit-End-Of-Day-EOD-Maximum-Trailing-Drawdown
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15171769361053-PRO-Account-Rules
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15169066911133-Rule-2-Do-Not-Exceed-Maximum-Position-Size

## Cenários considerados na estimativa inicial

Esta tabela foi produzida antes da definição da meta de 20 pregões e permanece apenas
como registro histórico. Ela não determina mais o tamanho da posição.

| Risco por operação | % do drawdown | % do limite diário | Três stops | MNQ aceitos | MES aceitos |
|---:|---:|---:|---:|---:|---:|
| USD 50 | 3,3% | 16,7% | USD 150 | 0 de 40 | 31 de 40 |
| USD 60 | 4,0% | 20,0% | USD 180 | 1 de 40 | 36 de 40 |
| USD 75 | 5,0% | 25,0% | USD 225 | 7 de 40 | 40 de 40 |
| USD 90 | 6,0% | 30,0% | USD 270 | 15 de 40 | 40 de 40 |
| USD 100 | 6,7% | 33,3% | USD 300 | 20 de 40 | 40 de 40 |

## Recomendação inicial — substituída

- risco técnico máximo do candidato instalado: USD 50 por microcontrato;
- não iniciar uma avaliação usando o candidato instalado;
- não definir quantidade multiplicando o resultado médio até alcançar a meta;
- limite diário e risco operacional serão definidos somente depois de existir um
  candidato economicamente aprovado;
- sinal acima de USD 50 por microcontrato: descartar, sem aproximar artificialmente o stop;
- sinal descartado: registrar no CSV como `RiskRejected`;
- não usar o máximo de 30 micros permitido pela mesa como referência de tamanho de posição.

O indicador não acessa saldo, pico intradiário, drawdown restante ou ordens reais.

## Impacto esperado na amostra atual

- MNQ: 7 de 40 sinais seriam elegíveis;
- MES: 40 de 40 sinais seriam elegíveis;
- o exemplo de aproximadamente USD 154 de risco no MNQ seria descartado;
- o filtro favorece o ativo que oferece stop técnico compatível com o orçamento financeiro.

## Portão econômico atual

Tipo inicial confirmado: conta de avaliação (`Test`).

1. atingir USD 1.500 em no máximo 20 pregões;
2. respeitar drawdown trailing de USD 1.500;
3. obter pelo menos 60% de aprovação nas janelas históricas;
4. limitar falhas por drawdown a no máximo 15%;
5. confirmar o resultado fora da amostra de seleção.

O `QualifiedPullback` foi reprovado nesse portão. Consulte
`docs/data-audits/2026-07-29-prop-evaluation-20d.md`.
