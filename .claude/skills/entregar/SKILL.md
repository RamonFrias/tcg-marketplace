---
name: entregar
description: Entrega o trabalho do tcg-marketplace pelo fluxo trunk-based. Roda build e testes, cria a branch, faz commit no padrão Conventional Commits em português e abre o pull request no GitHub. Use quando o usuário pedir para commitar, entregar, subir, abrir PR ou finalizar uma tarefa.
argument-hint: [resumo opcional da entrega]
disable-model-invocation: true
---

# Entregar (commit + PR)

Só rode quando o usuário pedir. Resumo informado: `$ARGUMENTS`.

## 1. Conferir antes

- `git status` e `git diff --stat`: liste ao usuário o que vai entrar.
- **Fora do commit:** segredos, `local.settings.json`, `.env`, `bin/`, `obj/` e arquivos sem relação com a tarefa. Pergunte sobre o que parecer estranho.
- **Build e testes:** `dotnet build tcg-marketplace.slnx` e `dotnet test tcg-marketplace.slnx`. Se falhar, **pare** e mostre o erro. Não se entrega vermelho.
- **ADR:** se a mudança tem decisão de arquitetura sem ADR, sugira a skill `adr` antes.
- **Revisão de design:** se a entrega tem entidade, caso de uso, evento ou saga novos, ofereça rodar antes os agents `arquiteto` (design) e `documentador` (docs em dia). Eles são independentes e podem rodar em paralelo.

## 2. Branch

- Nunca commitar na `main`. O hook `dangerous-commands` bloqueia.
- Nome no formato `<tipo>/<servico>-<descricao-curta>`. Exemplos: `feat/inventory-reserva-estoque`, `fix/catalog-busca-por-colecao`, `chore/fase0-aspire`.
- Tipos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `ci`.
- Comando: `git switch -c <branch>`. Se já estiver numa branch de trabalho, use ela.

## 3. Commit

Conventional Commits em português:
```
<tipo>(<escopo>): <o que mudou, no imperativo, até 72 caracteres>

<por que mudou e o que muda para quem usa, se não for óbvio>

Refs: <ADR ou issue, se houver>
```
- Escopo é o serviço em minúsculas (`catalog`, `inventory`) ou a área (`building-blocks`, `apphost`, `gateway`, `hooks`).
- Um commit por assunto. Se o diff mistura assuntos, proponha dividir.
- `git add` só dos arquivos da tarefa, por caminho (nada de `git add -A` às cegas).
- Siga as linhas de atribuição que o ambiente indicar.

## 4. Push e PR

- **Primeiro push do repositório:** quando não há remoto configurado ou a `main` ainda não existe no GitHub, não faça. Mostre ao usuário os comandos para rodar no terminal dele:
  ```powershell
  gh repo create tcg-marketplace --private --source . --remote origin
  ```
  ```powershell
  git push -u origin main
  ```
- **Nos demais:**
  1. `git push -u origin <branch>`.
  2. `gh pr create --base main --title "<mesmo título do commit>" --body-file <arquivo temporário>`. O corpo leva:
     - **Resumo:** o que e por quê, em 2 a 4 linhas.
     - **Mudanças:** lista por serviço.
     - **Como testar:** comandos e chamadas.
     - **Checklist:** testes passando, ADR (se aplicável), sem segredos, migrations revisadas.
  3. Não faça merge nem ative auto-merge. Quem aprova é o usuário.

## 5. Ao terminar

Mostre ao usuário a branch, o commit (hash curto e título) e o link do PR.
