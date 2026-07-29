# ADR 0008 — Rejeitar candidatos do backtest de 58 dias

## Status

Aceita em 29/07/2026.

## Contexto

Os arquivos OHLCV de MNQ e MES permitem reconstruir sinais em todo o período, em vez
de depender apenas dos dias nos quais uma versão específica do indicador gravou logs.
Foram pesquisadas três famílias de entrada, nas duas direções e nos dois ativos.

## Decisão

- não adicionar uma estratégia de compra;
- não substituir a regra de venda atual por um dos candidatos pesquisados;
- não promover o `EvidencePullback` para execução;
- manter a versão atual congelada somente em acompanhamento prospectivo;
- exigir um novo conjunto sincronizado de pelo menos seis meses antes da próxima
  seleção;
- definir o protocolo seguinte antes de revelar seu período final.

## Motivo

Nenhum candidato passou no teste final cronológico. O candidato de compra de MNQ
perdeu USD 230,25 no teste; nenhum candidato de compra de MES sobreviveu às fases
anteriores. Os candidatos vendidos de MNQ e MES também perderam no teste final.

A regra atual foi positiva no trecho mais recente, mas negativa no total reconstruído.
Portanto, ainda não há estabilidade suficiente para uma alteração operacional.

## Consequências

- nenhuma mudança no comportamento ou na versão do indicador;
- nenhuma ordem automática ou acesso à conta;
- o simulador e o resultado bruto permanecem versionados para reprodução;
- a próxima pesquisa começa com dados adicionais, e não com novos ajustes sobre o
  período final já observado.
