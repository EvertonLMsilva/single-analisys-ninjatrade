# Decisao 0017 - Manter momentum congelado apos 177 dias

Data: 2026-07-29.

## Contexto

Foram fornecidos novos arquivos de MNQ e MES com inicio em fevereiro e corte final
em 29/07. A regra congelada foi reexecutada sem ajuste de parametros.

## Evidencia

O resultado do MNQ passou de 81 para 100 operacoes e de USD 1.244 para USD 1.633
por micro. O profit factor passou de 1,4161 para 1,4746, enquanto o drawdown maximo
permaneceu em USD 621.

O portao economico continuou reprovado. A melhor configuracao dentro de USD 300 de
risco atingiu 45,68% de aprovacao e drawdown P90 de USD 1.223.

## Decisao

- manter exatamente a regra `intraday-momentum-2026-07-v1`;
- nao substituir a regra pelo melhor candidato encontrado na nova busca;
- nao aumentar a quantidade acima de um micro hipotetico;
- manter o inicio prospectivo depois de 29/07;
- usar a amostra ampliada somente como evidencia historica adicional;
- nao alterar o indicador neste incremento.

## Consequencia

A hipotese historica ficou mais forte, mas o objetivo economico de USD 1.500 em 20
pregoes ainda nao possui probabilidade suficiente. O proximo resultado decisivo
continua sendo a amostra prospectiva congelada.
