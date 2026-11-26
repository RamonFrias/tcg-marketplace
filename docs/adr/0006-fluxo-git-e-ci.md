# 0006. Fluxo trunk-based com PR revisado e CI no GitHub Actions

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos

## Contexto

O código é escrito em boa parte com o Claude Code, e o dono do projeto quer revisar no GitHub tudo o que entra na `main` antes do merge.

## Decisão

- **Trunk-based:** branches curtas `<tipo>/<servico>-<descricao>` (`feat`, `fix`, `refactor`, `test`, `docs`, `chore`...) saindo da `main`.
- **PR obrigatório:** a regra "Proteger main" no GitHub recusa push direto, force push e exclusão da `main`, e exige conversas resolvidas. O merge é feito pelo dono do repositório, depois de revisar o diff. O Claude nunca faz merge nem ativa auto-merge.
- **Commits:** Conventional Commits em português (`feat(catalog): cria agregado Card`).
- **CI** (`.github/workflows/ci.yml`) em todo PR e push na `main`:
  - build e testes da solution em Release;
  - testes dos hooks do Claude Code.
- **Matriz por serviço:** quando os serviços existirem, o job .NET ganha uma matriz por serviço com filtro de caminhos, para rodar só o que mudou. Até lá, a solution é pequena e roda inteira.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| GitFlow | Branches longas (develop/release) não ajudam num projeto com um dono e entregas semanais |
| Exigir aprovação formal no PR | O GitHub não deixa aprovar o próprio PR; o merge manual pelo dono já é a aprovação |

## Consequências

- **Mais fácil:** toda mudança é revisável e reversível; o CI pega quebra antes do merge.
- **Mais difícil:** até a mudança mais simples passa por PR.
- **A fazer:** depois do primeiro CI verde, exigir os checks `Build e testes (.NET)` e `Testes dos hooks do Claude Code` na regra da `main`.
