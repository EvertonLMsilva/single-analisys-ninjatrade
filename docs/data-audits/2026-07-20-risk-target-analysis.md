# Análise de risco e alcance dos alvos — 2026-07-20

## Objetivo

Investigar por que o MNQ apresentou aproximadamente 77 pontos de stop e 154 pontos de alvo, valores considerados altos para uma conta de mesa proprietária.

## Origem dos níveis

A configuração atual utiliza:

- stop: `ATR × 1,5`;
- alvo: `2 × distância do stop`;
- validade: 3 candles de 5 minutos;
- MNQ: USD 2 por ponto;
- MES: USD 5 por ponto.

Assim, um stop de aproximadamente 77 pontos no MNQ representa cerca de USD 154 por contrato. Com alvo de 2R, 154 pontos representam cerca de USD 308 antes de custos.

## Distribuição do risco

| Ativo | Mínimo | 25% | Mediana | Média | 75% | Máximo |
|---|---:|---:|---:|---:|---:|---:|
| MNQ | USD 58,50 | USD 84,00 | USD 100,00 | USD 111,24 | USD 133,50 | USD 204,00 |
| MES | USD 16,25 | USD 26,25 | USD 33,75 | USD 38,38 | USD 45,00 | USD 67,50 |

## Efeito de possíveis limites

| Limite por contrato | MNQ aceitos | MNQ rejeitados | MES aceitos | MES rejeitados |
|---:|---:|---:|---:|---:|
| USD 50 | 0 de 40 | 40 | 31 de 40 | 9 |
| USD 75 | 7 de 40 | 33 | 40 de 40 | 0 |
| USD 100 | 20 de 40 | 20 | 40 de 40 | 0 |
| USD 125 | 28 de 40 | 12 | 40 de 40 | 0 |
| USD 150 | 32 de 40 | 8 | 40 de 40 | 0 |

Esses cenários não definem qual limite é adequado. O valor deve ser compatível com as regras da mesa, o drawdown disponível e o plano de risco do operador.

## Movimento favorável observado

Dentro do acompanhamento atual:

| Movimento favorável | MNQ | MES |
|---:|---:|---:|
| pelo menos 0,5R | 23 de 40 | 23 de 40 |
| pelo menos 1,0R | 6 de 40 | 9 de 40 |
| pelo menos 1,5R | 3 de 40 | 4 de 40 |
| pelo menos 2,0R | 0 de 40 | 3 de 40 |

O MFE isolado não informa com certeza se um alvo alternativo teria ocorrido antes do stop em todos os casos, especialmente quando níveis diferentes são tocados no mesmo candle. Ele serve para orientar um novo teste, não para recalcular retrospectivamente o resultado final.

## Conclusões

1. a combinação `ATR 1,5 + alvo 2R + validade de 3 candles` está produzindo níveis grandes para o MNQ nesta amostra;
2. reduzir automaticamente o stop para caber em um limite financeiro pode invalidar o stop técnico;
3. a política recomendada é manter o stop técnico e classificar como descartado qualquer sinal acima do risco máximo configurado;
4. sinais descartados devem continuar registrados no CSV para medir quantas oportunidades foram filtradas;
5. alvo e validade precisam ser comparados separadamente, sem modificar os dois ao mesmo tempo;
6. o limite exato depende das regras da mesa proprietária e da conta utilizada.

## Próxima implementação proposta

- transformar o limite atual, que apenas informa, em uma política configurável;
- modos: `Somente avisar` e `Descartar acima do limite`;
- não tratar sinal descartado como operação ativa;
- registrar `RiskRejected` no CSV com o risco que causou o descarte;
- manter o stop técnico original no registro;
- depois criar uma comparação controlada entre alvos de 1R, 1,5R e 2R.

Antes de definir o valor padrão, registrar nome da mesa, tamanho da conta, drawdown máximo ou trailing e limite diário aplicável.
