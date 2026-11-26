# 0000. Registro de decisões de arquitetura

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos

## Contexto

O projeto vai ter vários serviços, padrões e trocas de tecnologia ao longo das fases. Sem registro, o motivo de cada escolha se perde e a mesma discussão volta.

## Decisão

Registramos cada decisão de arquitetura em `docs/adr/NNNN-titulo-em-kebab-case.md`, com numeração sequencial de 4 dígitos e as seções Contexto, Decisão, Alternativas consideradas e Consequências (modelo em `.claude/skills/adr/template.md`).

- ADR aceito não é reescrito. Se a decisão muda, um ADR novo a substitui e o antigo passa a `Substituído por NNNN`.
- O índice fica em [README.md](README.md).
- As decisões iniciais vieram da página de decisões do projeto (link em `.claude/project.json`).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Só o README | Mistura "como usar" com "por que é assim" e não guarda histórico |
| Wiki do GitHub | Fica fora do repositório e não passa por revisão de PR |

## Consequências

- **Mais fácil:** entender o porquê de cada escolha; revisar decisões no mesmo PR do código.
- **Mais difícil:** exige disciplina de escrever o ADR junto com a mudança (o hook `adr-reminder` lembra).
