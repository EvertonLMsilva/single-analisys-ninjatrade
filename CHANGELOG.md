# Histórico de versões

Todas as mudanças relevantes do projeto são registradas neste arquivo. Enquanto o indicador estiver em validação, as versões usarão o sufixo `beta`.

## 0.3.0-beta.1 — 2026-07-20

- adicionada gravação persistente dos sinais hipotéticos em CSV;
- criado um arquivo separado por dia, ativo e período gráfico;
- o registro ativo é atualizado quando o sinal atinge alvo, stop ou expira;
- adicionada chave estável por sinal e configuração para impedir duplicações ao recarregar o gráfico;
- registrados versão, parâmetros, preços, resultado em R, MFE, MAE e quantidade de candles;
- adicionada propriedade **Salvar histórico CSV** e situação da gravação no painel;
- validação automatizada do CSV e compilação estrutural concluídas sem erros.

Commit principal: `5316843`.

## 0.2.0-beta.1 — 2026-07-20

- redesenhada a apresentação visual dos sinais;
- adicionadas zonas de risco e retorno, cores distintas e limitação do histórico visual;
- adicionada a versão atual no cabeçalho do painel;
- mantido o modo exclusivamente visual, sem execução de ordens.

Commits principais: `2bfddac` e `11edee2`.

## 0.1.0-beta.1 — 2026-07-20

- criada a base mínima do indicador para NinjaTrader 8;
- adicionada regra demonstrativa de cruzamento de EMA;
- adicionados stop por ATR, alvo por risco/retorno e validade em candles;
- adicionado acompanhamento hipotético de alvo, stop, expiração e resultado ambíguo;
- corrigido o namespace de `DashStyleHelper` para compilação no NinjaTrader;
- confirmado por inspeção que não existem chamadas de execução ou gerenciamento de ordens.

Commits principais: `537d087`, `eae4d9f` e `ca2c4e1`.
