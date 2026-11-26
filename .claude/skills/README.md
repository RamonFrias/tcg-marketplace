# Skills do Claude Code · tcg-marketplace

Roteiros que o Claude segue quando a tarefa bate com a descrição. Também podem ser chamados pelo nome, por exemplo `/novo-servico Inventory`.

| Skill | Para quê | Fase |
|---|---|---|
| `novo-servico` | Esqueleto de um microsserviço: 4 camadas, testes, banco, AppHost e gateway | 0–1 |
| `nova-entidade` | Agregado, entidade ou value object de domínio com TDD | 1+ |
| `novo-caso-de-uso` | Endpoint HTTP → command/query → repositório, com testes | 1+ |
| `novo-evento` | Evento de integração: contrato, Outbox, consumidor idempotente | 1+ |
| `nova-migration` | Migration EF Core revisada contra perda de dados | 1+ |
| `importar-cards` | Seed do Catalog e importação da Scryfall e da pokemontcg.io | 1 |
| `nova-saga` | Orquestração com Durable Functions e compensações | 2 |
| `adr` | Registro de decisão em `docs/adr/` | todas |
| `entregar` | Build, testes, branch, Conventional Commit e PR (só quando o usuário pedir) | todas |
| `rodar-local` | Subir o Aspire e diagnosticar emuladores | todas |

As skills seguem as regras que os hooks em `.claude/hooks` cobram: camadas, contratos, segredos e trunk-based.
