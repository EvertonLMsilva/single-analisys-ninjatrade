# Registro de trabalho

## 2026-07-20

### Preparação do repositório

- validada a branch padrão `master`;
- criada e utilizada a branch `agent/initial-trade-assistant`;
- aberto o pull request em modo rascunho `#2`;
- mantida uma pasta oficial do projeto e uma cópia instalada no NinjaTrader.

### Base do assistente

- criado indicador exclusivamente visual;
- adicionada regra demonstrativa por cruzamento de EMA;
- adicionados entrada hipotética, stop por ATR e alvo por risco/retorno;
- confirmada ausência de execução automática e acesso à conta.

### Acompanhamento e visual

- adicionados alvo atingido, stop atingido, expiração e resultado ambíguo;
- adicionadas métricas em R, MFE e MAE;
- redesenhados painel, linhas, setas e zonas de risco e retorno;
- corrigida a referência de `DashStyleHelper` para compilação no NinjaTrader;
- adicionada a versão no cabeçalho do indicador.

### Histórico persistente

- criada gravação CSV por dia, ativo e período gráfico;
- implementada atualização de uma única linha durante o ciclo do sinal;
- adicionada proteção contra duplicação ao recarregar o gráfico;
- versão elevada para `0.3.0-beta.1`;
- testes do núcleo e do CSV passaram;
- compilação estrutural passou com zero erros e zero avisos;
- dez arquivos da instalação foram conferidos por SHA-256 após a cópia;
- commit `5316843` enviado para a branch de trabalho.

### Validação manual pendente

- compilar `0.3.0-beta.1` no NinjaScript Editor;
- confirmar `Histórico CSV: ATIVO`;
- gerar e encerrar um sinal em Playback;
- conferir criação, atualização e não duplicação do primeiro CSV.

### Correção de sinais sobrepostos

- observado no gráfico que um novo sinal podia aparecer durante uma operação hipotética ativa;
- definido o comportamento de no máximo uma operação ativa por vez;
- novos cruzamentos passam a ser ignorados até o encerramento do sinal atual;
- adicionada defesa no `SignalTracker` e no indicador;
- adicionado teste de rejeição do sinal sobreposto e aceitação após o encerramento;
- versão elevada para `0.3.1-beta.1`.

### Risco específico por instrumento

- analisada a diferença entre preço do índice, volatilidade em pontos e valor financeiro do contrato;
- decidido usar os metadados do instrumento carregado, sem valores fixos para MNQ ou MES;
- adicionados tick, valor do ponto, moeda, distância, risco e alvo financeiro;
- adicionado limite máximo por contrato, desativado por padrão até configuração do usuário;
- criado formato CSV v2 para preservar os arquivos anteriores;
- adicionados testes de arredondamento e de risco com valores de ponto diferentes;
- versão elevada para `0.4.0-beta.1`.

### Primeira auditoria dos dados v2

- inspecionados 10 arquivos e 78 registros entre 2026-07-15 e 2026-07-20;
- confirmada separação de 39 sinais para MNQ e 39 para MES;
- não foram encontradas chaves vazias ou duplicadas;
- confirmado risco médio de USD 110,14 no MNQ e USD 37,72 no MES;
- identificado que 62 de 78 sinais expiraram;
- arquivos v1 foram preservados, mas excluídos da comparação financeira;
- conclusão registrada em `docs/data-audits/2026-07-20-initial-v2-audit.md`.

### Auditoria após regeneração dos CSVs

- confirmada a presença exclusiva de 10 arquivos v2;
- analisados 80 registros, sendo 40 de MNQ e 40 de MES;
- confirmada ausência de duplicações, campos numéricos inválidos e inconsistências matemáticas;
- confirmados 78 sinais encerrados com os mesmos resultados da auditoria anterior;
- identificados dois sinais ativos, um por instrumento;
- conclusão registrada em `docs/data-audits/2026-07-20-regenerated-v2-audit.md`.

### Análise do risco e alcance do MNQ

- confirmada a origem do exemplo de aproximadamente 77 pontos de stop e 154 pontos de alvo;
- calculada mediana de risco de USD 100,00 e média de USD 111,24 no MNQ;
- simulada a quantidade de sinais permitidos por limites entre USD 50 e USD 150;
- identificado que nenhum sinal de MNQ alcançou 2R no acompanhamento atual;
- recomendada rejeição por risco em vez de redução automática do stop técnico;
- proposta registrada em `docs/data-audits/2026-07-20-risk-target-analysis.md`.

### Plano provisório para Take Profit Trader 25k

- confirmados drawdown informado de USD 1.500 e limite diário pessoal de USD 300;
- verificadas as diferenças entre trailing de avaliação e trailing intradiário da conta PRO;
- comparados limites de risco entre USD 50 e USD 100 sobre os logs atuais;
- recomendado provisoriamente USD 75 por operação, 1 micro e parada diária pessoal em USD 225;
- pendente confirmação do tipo de conta e da origem do limite diário;
- plano registrado em `docs/risk-plans/takeprofit-25k.md`.

### Correção sobre o limite diário

- esclarecido que a Take Profit Trader não impõe limite diário nessa conta;
- os USD 300 representam o orçamento máximo pessoal de perda por dia;
- mantida parada operacional recomendada em USD 225 para preservar USD 75 de margem;
- removida da documentação a pendência sobre a origem do valor diário.

### Filtro de risco para a avaliação 25k

- confirmado que o primeiro perfil será para conta `Test`;
- risco máximo padrão definido em USD 75 por contrato;
- adicionados modos de somente aviso e descarte acima do limite;
- sinal descartado permanece no CSV como `RiskRejected`, sem se tornar ativo;
- adicionada contagem no painel e marcação visual discreta;
- criado formato CSV v3 para registrar a política utilizada;
- versão elevada para `0.5.0-beta.1`.

### Diagnóstico da ausência de operações no MNQ

- analisados os CSVs v3 gerados com limite de USD 75;
- confirmadas 44 oportunidades, sendo 37 descartadas por risco;
- entre as 7 aceitas, ocorreram 5 expirações e 2 stops;
- confirmado que tendência sem novo cruzamento de EMA não gera sinal;
- descartada a hipótese de falha no filtro ou na gravação;
- diagnóstico registrado em `docs/data-audits/2026-07-20-mnq-risk-filter-behavior.md`.

### Decisão sobre ajuste do MNQ

- recomendado testar pullback a favor da tendência sem substituir a linha de base;
- mantidos limite de USD 75, referência de 1 micro e modo exclusivamente analítico;
- definidos critérios mínimos de comparação e validação em Playback;
- decisão registrada em `docs/decisions/0001-test-mnq-pullback.md`.

### Implementação do experimento de pullback

- criada a identidade de setup `TrendPullback` e `EmaCrossBaseline` no modelo do sinal;
- implementado pullback a favor da tendência com toque na região da EMA rápida, candle de confirmação e stop técnico;
- configurados valores iniciais de `0,1 ATR` para tolerância e 3 candles de intervalo mínimo por direção;
- mantido o cruzamento de EMA em paralelo apenas no CSV, sem elementos visuais;
- alterado o rastreador para permitir uma operação ativa por setup, preservando o bloqueio de sobreposição dentro do mesmo setup;
- painel passa a resumir exclusivamente os resultados do pullback;
- criado formato CSV v4 com coluna `Setup` e chave estável por setup;
- versão elevada para `0.6.0-beta.1`;
- testes do núcleo e do CSV passaram;
- compilação estrutural passou com zero erros e zero avisos;
- confirmada novamente a ausência de chamadas de execução de ordens.
- código sincronizado com a pasta oficial e com a instalação do NinjaTrader;
- os 12 arquivos instalados foram conferidos por SHA-256, sem divergências;
- compilação com F5 e validação visual no NinjaTrader permanecem como etapa manual seguinte.

### Validação da versão 0.6 no MNQ

- localizados 6 arquivos MNQ no formato CSV v4, com 205 registros;
- confirmada a versão `0.6.0-beta.1` em todos os registros;
- confirmada separação entre 161 sinais `TrendPullback` e 44 sinais `EmaCrossBaseline`;
- confirmados tick de 0,25, valor do ponto de USD 2 e limite de USD 75;
- entre os pullbacks, 102 ficaram dentro do limite e 59 foram rejeitados;
- resultados iniciais dos pullbacks: 15 alvos, 46 stops, 39 expirados e 2 ambíguos;
- taxa entre resultados decididos de 24,59% e total de -16R;
- validação detalhada registrada em `docs/data-audits/2026-07-20-mnq-pullback-v4-validation.md`;
- mantida a situação experimental, sem aprovação para operação real.

### Consolidação do dia 20

- comparados MNQ e MES nos dois setups durante todo o dia registrado;
- MNQ pullback: 23 aceitos, 12 rejeitados, 3 alvos, 9 stops, 10 expirados, 1 ambíguo e -3R;
- MES pullback: 39 aceitos, 1 rejeitado, 2 alvos, 12 stops, 25 expirados e -8R;
- estimado resultado bruto hipotético de -USD 285,50 no MNQ e -USD 198,75 no MES;
- confirmada utilidade da parada operacional pessoal em USD 225;
- identificada, sem aprovação, hipótese favorável entre 06:00 e 08:59 no horário do gráfico;
- identificado que 5 de 10 expirados do MNQ alcançaram pelo menos 1R de MFE;
- definida como próxima rodada a medição paralela de 1R, 1,5R e 2R sem alterar a entrada;
- análise registrada em `docs/data-audits/2026-07-20-full-day-analysis.md`.

## 2026-07-21

### Nova auditoria dos CSVs v4

- analisados 14 arquivos e 494 registros após a regeneração das 09:09;
- confirmadas versão, cabeçalhos, cálculos financeiros e ausência de duplicações dentro de cada arquivo;
- identificadas 137 chaves repetidas somente entre ativos diferentes, indicando a necessidade de incluir ativo e período no `RecordKey` consolidado;
- MNQ pullback acumulado: 192 candidatos, 129 aceitos, 19 alvos, 58 stops, 48 expirados, 3 ambíguos, 1 ativo e -20R;
- MES pullback acumulado: 202 candidatos, 200 aceitos, 20 alvos, 73 stops, 105 expirados, 2 ambíguos e -33R;
- parcial de 21/07: -4R no MNQ e -3R no MES;
- a hipótese de vantagem entre 06:00 e 08:59 no MNQ foi rejeitada no conjunto completo, com -4R;
- confirmado que 24 de 48 expirados no MNQ e 48 de 105 no MES alcançaram pelo menos 1R de MFE;
- mantida recomendação de não alterar entrada ou risco antes da medição paralela de 1R, 1,5R e 2R;
- auditoria registrada em `docs/data-audits/2026-07-21-v4-log-audit.md`.

## 2026-07-22

### Auditoria consolidada de todos os logs

- inventariados 36 arquivos: 10 v2, 10 v3 e 16 v4;
- formatos antigos preservados, mas excluídos do desempenho para evitar múltipla contagem;
- analisados 595 registros v4, todos na versão `0.6.0-beta.1`;
- confirmadas integridade interna, uniformidade dos cabeçalhos e exatidão dos cálculos financeiros;
- identificadas 150 chaves repetidas somente entre ativos diferentes;
- dias completos: MNQ pullback em -22R e MES pullback em -49R;
- parcial de 22/07 separado: -1R no MNQ e -2R no MES pullback;
- nenhuma hipótese de horário ou direção demonstrou robustez;
- faixa MNQ até USD 25 ficou em +3R, mas concentrada apenas nos dois últimos dias e sem aprovação como filtro;
- maior sequência observada: 12 stops no MNQ e 18 no MES;
- simulada parada pessoal de USD 225, que reduziu perdas, mas não tornou o setup positivo;
- mantida recomendação de alterar apenas a medição de 1R, 1,5R e 2R;
- relatório registrado em `docs/data-audits/2026-07-22-all-logs-audit.md`.

### Implementação da medição de múltiplos alvos

- mantidas sem alteração as regras de entrada, stop, validade, risco e setups;
- criada medição independente de 1R, 1,5R e 2R;
- adicionados preço, situação e horário para cada nível;
- registrado o primeiro evento observado;
- preservada ambiguidade quando alvo e stop aparecem no mesmo candle;
- painel passa a declarar explicitamente `RESULTADO HIPOTÉTICO | SEM ORDENS`;
- criado CSV v5 com hipótese de entrada e base do cálculo;
- ativo e período adicionados ao `RecordKey`;
- versão elevada para `0.7.0-beta.1`;
- decisão registrada em `docs/decisions/0002-measure-multiple-targets.md`;
- testes do rastreador e do CSV passaram;
- compilação estrutural passou com zero erros e zero avisos.
- código sincronizado com a pasta oficial e com a instalação do NinjaTrader;
- os 14 arquivos instalados foram conferidos por SHA-256, sem divergências;
- compilação com F5 e validação visual do primeiro CSV v5 permanecem como etapas manuais.

### Início da validação prospectiva 0.8

- encerrada a etapa de descoberta com 540 registros v5 entre 15 e 22/07;
- selecionado MES `EmaCrossBaseline` em 1R como candidato;
- mantido MNQ `TrendPullback` em 1,5R apenas em observação;
- pausado visualmente MES `TrendPullback`, sem interromper sua coleta silenciosa;
- criada rodada congelada `forward-2026-07-v1` com bloqueio de novos sinais em configuração divergente;
- painel adaptado ao setup principal de cada ativo e às métricas diárias do alvo escolhido;
- criado CSV bruto v6 com contexto completo da validação;
- criado resumo diário automático com resultado em R e moeda, riscos médios, sequência de stops e drawdown;
- definida data de corte em 23/07; recálculos anteriores são marcados como referência histórica e inelegíveis;
- adicionados testes executáveis do núcleo para perfis, configuração congelada, métricas e persistência;
- versão elevada para `0.8.0-beta.1`;
- decisão registrada em `docs/decisions/0003-start-forward-validation.md`;
- análise que sustentou a seleção registrada em `docs/data-audits/2026-07-22-v5-forward-selection.md`;
- testes do núcleo concluídos com sucesso;
- projeto real `NinjaTrader.Custom` compilado pelo terminal com zero erros; os avisos exibidos pertencem ao conjunto geral de scripts já instalado;
- código sincronizado com o repositório oficial e com a pasta `bin/Custom/TradeAssistant`;
- mantida ausência total de ordens e acesso à conta;
- validação visual no gráfico e confirmação dos primeiros CSVs v6/resumos permanecem como etapa manual.

## 2026-07-28

### Encerramento da rodada v1 e integridade dos logs

- auditados os arquivos v6 de 23 a 28/07 e identificada diferença de cinco registros entre CSVs brutos e resumos atuais;
- resultado dos perfis principais: MES -1R/-USD 52,50 e MNQ +2R/-USD 105,25, antes de custos;
- rodada `forward-2026-07-v1` encerrada sem aprovação operacional;
- criada versão `0.8.1-beta.1` e rodada diagnóstica `diagnostic-2026-07-v2`;
- preservados os arquivos v6 e `validation_v1`;
- criado CSV v7 com substituição do retrato diário para remover registros órfãos em reprocessamentos;
- criado resumo `validation_v2`;
- criada análise `segments_v1` por direção, hora e faixa de risco;
- entradas, stops, alvos, validade e limite financeiro permaneceram inalterados;
- adicionados testes de sincronização e segmentação;
- testes do núcleo concluídos com sucesso;
- código sincronizado com `bin/Custom/TradeAssistant`;
- projeto `NinjaTrader.Custom` compilado pelo terminal com zero erros; os avisos pertencem ao conjunto geral de scripts instalado;
- implementação registrada no commit `99a4791`;
- pull request aberto em `https://github.com/EvertonLMsilva/single-analisys-ninjatrade/pull/3`;
- validação visual e confirmação dos novos arquivos no NinjaTrader permanecem como etapas manuais.

### Redesenho da hipótese com todos os CSVs v7

- consolidados 16 arquivos v7 e 504 registros entre 20 e 28/07;
- confirmadas ausência de duplicações e consistência dos cálculos financeiros;
- rejeitados como candidatos MES pullback, MNQ baseline e MNQ pullback misturando as duas direções;
- identificado no MNQ pullback em 1R: compras em -12R/-USD 859,50 e vendas em +16R/+USD 528,00;
- definida como hipótese candidata MNQ vendido, risco máximo de USD 50 e alvo de 1R;
- candidato diagnóstico: 34 decisões, 24 alvos, 10 stops, +14R, +USD 428,00 e drawdown de USD 124,50;
- resultado separado em +USD 306,50 antes de 26/07 e +USD 121,50 entre 26 e 28/07;
- mantida a exigência de nova validação prospectiva porque a regra foi selecionada sobre a própria amostra;
- auditoria registrada em `docs/data-audits/2026-07-28-v7-strategy-redesign.md`;
- nenhuma regra do indicador foi alterada nesta etapa.

### Implementação do pullback contextual 0.9

- identificado que a regra anterior usava apenas relação e inclinação imediata das EMAs, toque na EMA rápida e cor do candle;
- criado `ContextPullback` com critérios simétricos de compra e venda;
- adicionada VWAP aproximada por sessão, reiniciada pelo template de horário do gráfico;
- adicionados inclinação da VWAP, inclinação de três candles das EMAs, corpo/localização do candle, distância em ATR e volume relativo;
- definido score mínimo de 5/6, com lado da VWAP, inclinação, EMAs, candle e extensão obrigatórios;
- mantido `TrendPullback` invisível como referência;
- risco máximo reduzido para USD 50 e alvo de validação definido em 1R;
- criado CSV v8, resumo `validation_v3` e segmentos `segments_v2`;
- painel passa a mostrar VWAP, distância, volume e justificativa do contexto;
- rodada `context-2026-07-v3` definida para iniciar em 29/07;
- decisão registrada em `docs/decisions/0005-test-context-pullback.md`;
- nenhuma execução automática ou acesso à conta foi adicionado.
- testes do núcleo concluídos com sucesso, incluindo contextos comprador e vendedor;
- código sincronizado com `bin/Custom/TradeAssistant` e conferido por hash;
- projeto `NinjaTrader.Custom` compilado pelo terminal com zero erros;
- implementação registrada no commit `a06060e`;
- pull request aberto em `https://github.com/EvertonLMsilva/single-analisys-ninjatrade/pull/4`;
- validação visual, F5 e confirmação dos primeiros arquivos v8 permanecem como etapas manuais.

### Correção do cache de compilação do NinjaTrader

- analisado o arquivo `hoje.csv`, contendo 62 erros `CS0579`;
- identificados 56 erros em arquivos `.resources.cs` gerados para oito idiomas e seis erros refletidos em `AssemblyInfo.cs`;
- confirmada ausência de erro no código do indicador;
- executada a limpeza do projeto `NinjaTrader.Custom`, removendo somente artefatos gerados em `bin/Debug` e `obj/Debug`;
- confirmado que não restaram arquivos `.resources.cs` nem referências de compilação para `obj`;
- mantidos intactos o indicador instalado, as configurações e os logs;
- processo de desenvolvimento atualizado para exigir limpeza após a compilação pelo terminal e antes do F5.

## 2026-07-29

### Correção do bloqueio da rodada contextual

- confirmado que a versão 0.9 estava instalada e que os sinais de compra e venda permaneciam habilitados;
- identificado no workspace `Mercado americano` que MES e MNQ ainda conservavam `MaximumRiskPerContract = 75`;
- confirmado que a rodada contextual exige USD 50 e, por isso, a validação bloqueava toda a avaliação antes dos filtros de tendência e VWAP;
- adicionada migração automática que reduz limites antigos acima de USD 50 para o limite congelado quando a política é descartar acima do limite;
- limites inferiores a USD 50 e políticas diferentes não são relaxados automaticamente;
- mantidos os critérios de entrada, o início prospectivo em 29/07 e o formato CSV v8;
- versão elevada para `0.9.1-beta.1`.

### Seleção offline da estratégia 1.0

- consolidados 14 arquivos v8, 531 registros e 380 candidatos de pullback entre 22 e 29/07;
- separados os dados de 22 a 26/07 para seleção e de 27 a 29/07 para verificação posterior;
- custos estimados em USD 3 por operação, ambiguidades tratadas como perda e risco limitado a USD 50;
- nenhuma regra simples do MES permaneceu positiva nos dois períodos com amostra mínima;
- compras de MNQ e combinação das duas direções não apresentaram estabilidade suficiente;
- selecionado MNQ vendido, abaixo de VWAP descendente e score mínimo 4/6;
- resultado retrospectivo: 23 sinais, 15 alvos, quatro stops, duas ambiguidades, duas expirações, USD 272 líquidos estimados, profit factor 2,23 e drawdown de USD 85;
- criado setup `EvidencePullback` como único candidato visível;
- mantidos MES, compras e setups anteriores como pesquisa silenciosa;
- criada rodada `evidence-2026-07-v4`, iniciando em 30/07 e congelada por pelo menos dez resultados decididos;
- versão elevada para `1.0.0-beta.1`;
- análise registrada em `docs/data-audits/2026-07-29-evidence-strategy-selection.md`;
- decisão registrada em `docs/decisions/0006-select-evidence-pullback.md`.
- testes do núcleo concluídos com sucesso;
- confirmada ausência de chamadas de execução de ordens ou acesso à conta;
- código sincronizado com `bin/Custom/TradeAssistant` e conferido por hash;
- projeto `NinjaTrader.Custom` compilado com zero erros;
- cache de compilação limpo após o teste, sem arquivos `.resources.cs` residuais.
