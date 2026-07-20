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
