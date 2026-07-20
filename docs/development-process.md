# Processo de desenvolvimento

Este processo é obrigatório para cada incremento do Trade Assistant.

## Antes de alterar

1. definir um objetivo pequeno e verificável;
2. registrar o comportamento atual que será preservado;
3. identificar riscos, principalmente qualquer possibilidade de acesso à conta ou envio de ordens;
4. confirmar que a árvore de trabalho está limpa.

## Durante a alteração

1. manter análise, acompanhamento, persistência e desenho separados;
2. não adicionar execução automática de ordens;
3. criar ou atualizar testes proporcionais ao risco da mudança;
4. atualizar a versão quando houver mudança visível ou no formato dos dados;
5. atualizar `CHANGELOG.md` e a documentação relacionada no mesmo commit.

## Antes de publicar

1. executar os testes do núcleo;
2. executar a compilação estrutural do indicador;
3. procurar chamadas proibidas de execução e acesso à conta;
4. revisar o diff e confirmar que contém somente arquivos do incremento;
5. criar um commit com descrição objetiva;
6. enviar para a branch de trabalho e manter o pull request atualizado;
7. sincronizar a pasta oficial e a instalação do NinjaTrader;
8. validar a compilação real com F5 no NinjaScript Editor.

## Registro obrigatório de cada versão

Cada versão deve informar:

- objetivo;
- arquivos ou módulos alterados;
- comportamento novo;
- testes executados e seus resultados;
- limitações conhecidas;
- instruções de validação manual;
- commit e pull request relacionados;
- próximo passo recomendado.

Uma versão só pode ser considerada validada depois da compilação real no NinjaTrader e da confirmação do comportamento no gráfico.
