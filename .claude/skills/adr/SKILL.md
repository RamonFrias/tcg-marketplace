---
name: adr
description: Registra uma decisão de arquitetura (ADR) do tcg-marketplace em docs/adr/ com numeração automática e as seções Contexto, Decisão, Alternativas e Consequências. Use quando pedirem para registrar ou documentar uma decisão, quando o hook adr-reminder avisar, ou ao final de mudanças em pacotes, serviços, contratos, sagas ou infraestrutura.
argument-hint: <título da decisão>
---

# Registrar ADR

Decisão: `$ARGUMENTS`.

## Quando vale um ADR

Vale para algo que outra pessoa do time perguntaria "por que fizeram assim?":
- tecnologia ou pacote novo;
- serviço novo;
- contrato de evento novo ou alterado;
- saga;
- padrão adotado ou abandonado;
- troca de uma decisão anterior.

Ajuste pequeno de código não vale ADR. Se for o caso, diga isso ao usuário.

## Passo a passo

1. Liste `docs/adr/` e pegue o próximo número com 4 dígitos (`0001`, `0002`...). Crie a pasta se não existir. Se for o primeiro, crie antes `0000-registro-de-decisoes.md` explicando o formato.
2. Nome do arquivo: `docs/adr/NNNN-titulo-em-kebab-case.md`, em português e sem acentos no nome.
3. Preencha o modelo [template.md](template.md):
   - **Contexto:** o problema e as restrições reais do projeto (tudo local, Azure Functions, estudo que pode ir para o Azure).
   - **Decisão:** frase direta no presente ("Usamos Outbox em cada serviço para...").
   - **Alternativas consideradas:** pelo menos uma, com o motivo de não ter sido escolhida.
   - **Consequências:** o que fica mais fácil, o que fica mais difícil e o que precisa ser feito.
4. Status inicial `Aceito`, a não ser que o usuário diga que ainda está em discussão (`Proposto`).
5. Se substitui um ADR anterior:
   - no antigo, mude o status para `Substituído por NNNN` (só a linha de status; o resto do ADR não muda);
   - no novo, cite o antigo.
6. Atualize o índice `docs/adr/README.md`: tabela com número, título, status e data.

## Decisões já tomadas

As decisões iniciais do projeto estão na página de decisões (link em `.claude/project.json` → `decisoes`). Quando um ADR formalizar uma delas, cite que veio de lá.

Mostre ao usuário o caminho do arquivo e um resumo de uma linha.
