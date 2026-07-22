# Decisão 0002 — Medir 1R, 1,5R e 2R sem alterar a entrada

Data: 2026-07-22.

Situação: implementada para coleta experimental.

## Contexto

A auditoria de 595 registros v4 mostrou resultado negativo nos setups atuais. Ao mesmo tempo, uma parte relevante dos sinais expirados alcançou 1R ou 1,5R de MFE sem atingir 2R dentro da validade de três candles.

MFE não informa a ordem intrabar. Quando alvo menor e stop aparecem no mesmo candle, não é possível concluir retroativamente qual ocorreu primeiro.

## Decisão

Manter entrada, stop técnico, validade, limite financeiro e regra de sinal sem mudanças. Adicionar somente medição paralela de 1R, 1,5R e 2R.

Para cada nível, registrar:

- preço ajustado ao tick;
- situação `Pending`, `TargetHit`, `StopHit`, `Expired`, `Ambiguous` ou `RiskRejected`;
- horário do evento;
- primeiro evento geral: 1R, stop, expiração, ambíguo ou rejeição.

Se alvo e stop forem tocados no mesmo candle antes de existir um evento anterior, o resultado correspondente será ambíguo. Nenhuma ordem intrabar será inventada.

## Persistência

O CSV v5 identifica explicitamente:

- `EvaluationType=Hypothetical`;
- entrada presumida no fechamento do candle do sinal;
- resultado calculado pelas máximas e mínimas dos candles seguintes;
- ativo e período dentro do `RecordKey`;
- resultados paralelos de 1R, 1,5R e 2R.

## Restrições mantidas

- nenhuma execução automática;
- nenhuma leitura de conta ou de ordens reais;
- risco máximo padrão de USD 75;
- resultado sem comissão, taxas ou slippage;
- arquivos v2, v3 e v4 preservados;
- nenhuma aprovação para operação real.
