# Seleção da rodada prospectiva a partir dos CSVs v5

Data da análise: 2026-07-22

## Escopo e integridade

- 14 arquivos v5 de MNQ e MES;
- 540 registros entre 15 e 22 de julho;
- todos na versão `0.7.0-beta.1`;
- nenhuma chave vazia ou duplicada;
- cálculos financeiros conferidos sem divergências;
- dia 15 contém apenas o trecho noturno e dia 22 é parcial;
- resultados hipotéticos, por um microcontrato, antes de comissão e slippage.

## Descobertas usadas na decisão

Considerando os dias anteriores a 22/07 e somando o parcial disponível de 22/07:

- MES `EmaCrossBaseline` em 1R: aproximadamente +USD 208,75 e +4R, com 24 resultados decididos;
- MES `TrendPullback` em 1R: aproximadamente -USD 361,25;
- MNQ `TrendPullback` em 1,5R: +8R, mas aproximadamente -USD 347,25.

A divergência do MNQ ocorre porque os riscos financeiros variam por sinal. Em 21/07, por exemplo, os vencedores do pullback em 1,5R arriscaram em média USD 18,00, enquanto os perdedores arriscaram em média USD 41,94. Portanto, resultado positivo em R não foi suficiente para produzir saldo positivo com um contrato.

O MES `EmaCrossBaseline` é apenas o melhor candidato relativo da amostra, não uma estratégia aprovada. A quantidade decidida ainda é pequena e muitos sinais expiraram.

## Decisão

- iniciar nova amostra prospectiva sem misturar os resultados usados na seleção;
- MES `EmaCrossBaseline` em 1R como candidato;
- MNQ `TrendPullback` em 1,5R somente em observação;
- MES `TrendPullback` pausado visualmente;
- manter coleta silenciosa dos setups de referência;
- congelar parâmetros por cinco sessões completas;
- revisar somente após pelo menos 30 resultados decididos por candidato;
- exigir resultado coerente em R e dólares, sem concentração excessiva em um único dia.

Consulte também `docs/decisions/0003-start-forward-validation.md`.
