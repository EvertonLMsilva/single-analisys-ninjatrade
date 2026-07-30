# Decisão 0007 — Preparar automação futura sem habilitar ordens

## Situação

Aceita em 29/07/2026.

## Contexto

O projeto poderá futuramente evoluir de assistente visual para execução assistida ou
automática. A versão atual ainda está validando a estratégia e não possui acesso à conta.
As regras atuais da Take Profit Trader PRO proíbem bots e algos e exigem execução manual.

## Decisão

Preservar quatro camadas independentes:

1. análise e criação do sinal;
2. acompanhamento hipotético;
3. execução simulada ou confirmação manual;
4. adaptador de conta real.

O adaptador real não será criado dentro do indicador atual. Qualquer implementação futura
deverá iniciar desligada, identificar explicitamente o ambiente e bloquear contas nas quais
automação não seja permitida.

## Portões mínimos

Antes de qualquer automação real:

- estratégia congelada aprovada prospectivamente;
- custos e slippage incluídos;
- Playback e Sim101 concluídos;
- prevenção de duplicidade e reconciliação de posição testadas;
- limites de risco e botão de emergência testados;
- regras da conta verificadas novamente em fonte oficial;
- autorização explícita do usuário para a conta e o ambiente específicos.

## Consequências

- nenhuma ordem será adicionada agora;
- a estratégia não dependerá da interface ou da corretora;
- execução simulada poderá ser desenvolvida sem risco de atingir uma conta real;
- contas PRO da Take Profit Trader permanecerão manuais enquanto a proibição atual existir.

