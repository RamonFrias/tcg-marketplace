# Agents do Claude Code · tcg-marketplace

Colegas com contexto próprio, chamados para uma tarefa e que devolvem só a conclusão.
Um agent entra só quando há ganho real: olhar independente, contexto limpo ou trabalho em paralelo.

| Agent | Para quê | Pode escrever | Modelo |
|---|---|---|---|
| `arquiteto` | Revisão independente de design: DDD, Clean Architecture, SOLID, fronteiras entre serviços, coerência com ADRs | nada (só lê) | o da sessão |
| `qa` | Procura casos de borda e escreve os testes unitários que faltam; reporta bugs sem corrigir produção | só `tests/` | o da sessão |
| `pesquisador-dotnet` | Consulta a documentação oficial atual de Aspire, Functions, Durable, Service Bus, EF Core e YARP para a versão do projeto | nada | sonnet |
| `documentador` | Mantém README, `docs/` (arquitetura, eventos, fluxos, serviços, como rodar) e o índice de ADRs em dia com o código | só `.md`, nunca ADR | sonnet |

Os limites são garantidos por hooks, não só pelo texto dos agents:
- **`protect-files`:** o `qa` só edita em `tests/`. O `documentador` só edita `.md` em `docs/` e arquivos `README.md`, nunca `.claude/`, `CLAUDE.md` ou ADRs (exceto o índice `docs/adr/README.md`).
- **`dangerous-commands`:** no terminal, `arquiteto` e `qa` só rodam git de leitura e `dotnet build/test`, e o `documentador` só git de leitura. Redirecionamento (`>`) e substituição de comando são recusados.
O revisor de bugs continua sendo o `code-reviewer` global.

Onde as skills chamam cada agent:
- `nova-entidade` e `novo-caso-de-uso` → `qa`
- `novo-servico` e `novo-evento` → `pesquisador-dotnet`
- `entregar` → oferece `arquiteto` e `documentador`
