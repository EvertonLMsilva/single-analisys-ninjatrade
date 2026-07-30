# 0004 — Encerrar a primeira rodada prospectiva e iniciar diagnóstico

Data: 2026-07-28

## Situação

A rodada `forward-2026-07-v1` acumulou resultado financeiro hipotético negativo nos dois perfis principais. O MES teve poucos resultados decididos e alta expiração. O MNQ superou o mínimo de decisões, mas permaneceu negativo em moeda antes de custos.

Também foi identificado que o mecanismo de upsert do CSV bruto não removia sinais antigos que deixavam de existir após um novo processamento histórico. Os resumos diários eram reescritos corretamente, criando divergência entre bruto e resumo.

## Decisão

- encerrar `forward-2026-07-v1` sem aprovação operacional;
- preservar os arquivos v6 e `validation_v1`;
- criar a versão 0.8.1-beta.1 e a rodada `diagnostic-2026-07-v2`;
- iniciar a nova amostra em 28/07/2026;
- usar CSV bruto v7 com sincronização do retrato completo de cada dia;
- gerar resumos `validation_v2` e segmentos `segments_v1`;
- não alterar regras de entrada, stop, alvo, validade ou limite de risco nesta entrega.

## Consequências

A versão nova permitirá localizar se o prejuízo está concentrado em direção, horário ou faixa de risco sem misturar registros órfãos. Nenhum segmento será promovido apenas por aparecer positivo em poucos dias. A próxima mudança de estratégia deverá conter uma única hipótese verificável por ativo e abrir uma rodada independente.

O indicador continua exclusivamente analítico, sem acesso à conta e sem execução de ordens.
