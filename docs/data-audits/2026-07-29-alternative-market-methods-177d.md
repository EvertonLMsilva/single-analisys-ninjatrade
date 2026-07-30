# Auditoria - momentum intradiario com 177 dias

Data: 2026-07-29.

## Objetivo

Reexecutar a pesquisa de metodos alternativos com os novos arquivos de um minuto,
sem alterar a regra congelada `intraday-momentum-2026-07-v1`.

Os arquivos terminam em 29/07/2026, como os anteriores. O periodo adicional fica
antes da amostra original; portanto, amplia a evidencia historica, mas nao constitui
validacao prospectiva posterior ao congelamento.

## Dados

| Ativo | Barras | Inicio | Fim | Intervalos de 1 minuto | Minutos ausentes dentro da hora |
|---|---:|---|---|---:|---:|
| MNQ | 174.229 | 01/02/2026 20:01 | 29/07/2026 17:29 | 99,9265% | 0 |
| MES | 175.183 | 01/02/2026 20:01 | 29/07/2026 17:29 | 99,9269% | 1 |

Foram encontrados 120 pregoes compartilhados entre 03/02 e 29/07. Depois das 20
sessoes de aquecimento, a avaliacao passou a conter 100 pregoes, de 04/03 a 29/07.

Hashes:

- MNQ:
  `77EA1DB90EB8CDBD1833BA2B6C4C498751B8BA0C3FBE19B12AC35B5E6B50CA94`;
- MES:
  `CBD8EA117052E25BE5B44C5D18701E938508DD2FAB0B4512CE57BD5165AA9EAE`.

## Regra mantida

- ativo MNQ;
- um sinal na abertura da ultima meia hora;
- alta volatilidade: direcao do fechamento anterior ao fim da primeira meia hora;
- baixa volatilidade: soma desse retorno ao retorno da penultima meia hora;
- stop de USD 75 por micro;
- saida no fechamento regular;
- custo hipotetico de USD 5;
- nenhum parametro novo ou reajustado.

## Resultado ampliado com um micro

| Metrica | 149 dias | 177 dias | Diferenca |
|---|---:|---:|---:|
| Operacoes | 81 | 100 | +19 |
| Resultado liquido | USD 1.244,00 | USD 1.633,00 | +USD 389,00 |
| Media por operacao | USD 15,36 | USD 16,33 | +USD 0,97 |
| Taxa de acerto | 46,91% | 51,00% | +4,09 p.p. |
| Profit factor | 1,4161 | 1,4746 | +0,0585 |
| Drawdown maximo | USD 621,00 | USD 621,00 | sem mudanca |
| Maior intervalo sem sinal | 0 | 0 | sem mudanca |

Resultado mensal da amostra ampliada:

- marco: +USD 440,00;
- abril: +USD 70,00;
- maio: +USD 430,00;
- junho: +USD 887,50;
- julho: -USD 194,50.

Quatro de cinco meses permaneceram positivos. Julho continua negativo, e junho
continua concentrando parte relevante do lucro.

## Por que o MES nao foi habilitado

A mesma combinacao de regimes aplicada ao MES produziu:

- 100 operacoes;
- -USD 204,73;
- media de -USD 2,05 por operacao;
- taxa de acerto de 41%;
- profit factor de 0,7075;
- drawdown maximo de USD 288,45;
- quatro de cinco meses negativos.

Por isso, os dados do MES continuam uteis para pesquisa e comparacao, mas essa regra
nao deve gerar analises no grafico do MES.

## Meta de USD 1.500 em 20 pregoes

O melhor dimensionamento dentro do teto de USD 300 por operacao permaneceu:

- 2 micros no regime de alta volatilidade;
- 4 micros no regime de baixa volatilidade.

Com a amostra ampliada:

- taxa de aprovacao: 45,68%;
- falha por drawdown: 8,64%;
- drawdown P90: USD 1.223,00;
- mediana para aprovacao: 12 sessoes.

O risco melhorou, mas a taxa de aprovacao caiu de 50,00% para 45,68%. O candidato
continua reprovado no portao economico de 60% de aprovacao e drawdown P90 de ate
USD 1.000.

## Reprodutibilidade

O programa foi executado duas vezes com self-tests. Os dois JSONs produziram o
mesmo SHA-256:

`8B85A2E24C0F144DE083C89F58088D4DBEED6EA8B42ECFF4836C1ED574E63390`.

## Conclusao

Os 19 pregoes adicionais reforcam a vantagem historica da regra congelada, sem
aumentar o drawdown observado. Isso justifica manter a coleta prospectiva com um
micro, mas nao autoriza aumento de quantidade nem permite chamar a estrategia de
validada.

Nenhuma regra do indicador deve ser alterada com base nesta extensao. A decisao
continua dependendo das 20 sessoes posteriores a 29/07.

Artefato:

- `research/results/2026-07-29-alternative-market-methods-177d.json`.
