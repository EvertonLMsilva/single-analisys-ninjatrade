# Decisão 0013 — Reprovar o primeiro gatilho por regime e estrutura

Data: 2026-07-29.

## Contexto

Após a reprovação das combinações de indicadores e portfólios, foi criada uma linha
de pesquisa baseada em estado do mercado, faixa de abertura, VWAP, estrutura e
confirmação cruzada MNQ/MES. Foram usados dados sincronizados de um minuto.

## Decisão

- reprovar `TrendRetest` e `BalanceRejection` na forma atual;
- não publicar os novos setups no indicador;
- não modificar a versão instalada;
- não aumentar contratos para compensar ausência de vantagem;
- preservar a classificação de regime como infraestrutura de pesquisa;
- medir excursão favorável e adversa antes de propor o próximo gatilho;
- exigir dados bid/ask ou volumétricos para qualquer etapa que use fluxo de ordens;
- reservar dados posteriores a 29/07 para confirmação prospectiva.

## Evidência

O MNQ perdeu USD 846,76 em 30 operações, com PF 0,355 e resultado negativo nos três
blocos cronológicos. O MES ganhou USD 36,02 em apenas seis operações, uma amostra
insuficiente. Nenhuma quantidade de 1 a 30 micros passou pelo portão econômico de 20
pregões.

Detalhes: `docs/data-audits/2026-07-29-regime-structure-1m.md`.

## Consequência

O indicador continua no modo de pesquisa existente, sem execução automática. A nova
infraestrutura pode ser reutilizada, mas o próximo experimento precisa mudar o
gatilho, não apenas ajustar os números do mesmo reteste.
