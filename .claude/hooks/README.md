# Hooks do Claude Code · tcg-marketplace

Registrados em `.claude/settings.json`. Todos são scripts Node (sem dependências), e **nenhuma falha é silenciosa**:

| Situação | Hooks de guarda (1, 2, 3, 8) | Hooks de aviso (os demais) |
|---|---|---|
| Regra violada | exit 2 + motivo no stderr: a ação é bloqueada | aviso no contexto ou `systemMessage` |
| Erro interno (entrada inválida, exceção) | exit 2: bloqueia por segurança (fail-closed) | exit 1 + mensagem: aparece como erro, a ação segue |
| O processo nem roda (Node ausente, timeout) | `onFailure: "block"` no settings bloqueia | erro reportado pelo Claude Code |
| `dotnet` ausente | — | `systemMessage` avisando que não formatou/compilou |

Para ver o stack trace de um erro interno: `TCG_HOOKS_DEBUG=1`.

| # | Script | Evento | Efeito |
|---|---|---|---|
| 1 | `protect-files.js` | PreToolUse (Write/Edit) | **Bloqueia** escrita em `.git/`, `bin/`, `obj/`, `.env` reais e qualquer segredo (senha, AccountKey, SharedAccessKey, chave privada, segredo em JSON). Aceita placeholders (`<senha>`, `${VAR}`) e as chaves públicas dos emuladores. Também limita os agents: `qa` só em `tests/`, `documentador` só em `.md` de `docs/` e `README.md` (nunca `.claude/`, `CLAUDE.md` ou ADR). |
| 2 | `layer-guard.js` | PreToolUse (Write/Edit) | **Bloqueia** dependência errada em `src/services/<Servico>/<Servico>.<Camada>/`: Domain/Application usando infra, camada interna usando externa, serviço usando código de outro serviço. |
| 3 | `dangerous-commands.js` | PreToolUse (Bash/PowerShell) | **Bloqueia** push forçado, push/commit direto na `main` (entende `git switch -c x && git commit` e `push origin HEAD`), `reset --hard`, `clean -f`, `checkout .`, `stash clear`, apagar volumes do Docker e remoção recursiva de pastas raiz. Libera de propósito `--force-with-lease` e `rebase`. Para os agents `arquiteto`, `qa` e `documentador`, só permite git de leitura (e `dotnet build/test` para os dois primeiros). |
| 4 | `format-cs.js` | PostToolUse (Write/Edit) | Roda `dotnet format whitespace` só no `.cs` alterado. |
| 5 | `tdd-reminder.js` | PostToolUse (Write) | **Avisa** quando nasce classe em Domain/Application sem `<Classe>Tests.cs` em `tests/`. |
| 6 | `build-test-on-stop.js` (+ `track-changes.js`) | Stop | Compila e testa só os serviços alterados na sessão. Se falhar, o Claude continua corrigindo (até 3 vezes). |
| 7 | `adr-reminder.js` | PostToolUse (Write/Edit) | **Avisa** para registrar ADR em `docs/adr/` quando mudam pacotes, AppHost ou contratos. |
| 8 | `contracts-guard.js` | PreToolUse (Write/Edit) | **Pede confirmação** quando um campo ou tipo de um contrato de evento existente é removido ou renomeado. |
| 9 | `session-context.js` | SessionStart | Informa fase atual, branch, alterações pendentes e versões de dotnet/func/docker/node. |
| 10 | `notify.js` (+ `prompt-timer.js`) | Notification, Stop | Notificação do Windows quando o Claude espera você ou termina tarefa de 1 min ou mais. |

Configuração compartilhada em `.claude/project.json` (fase atual, lista de serviços, link das decisões).
Estado de sessão fica na pasta temporária do sistema (`%TEMP%\tcg-marketplace-hooks`), fora do repositório.

## Estrutura que os hooks esperam

```
src/
  services/<Servico>/<Servico>.Domain | .Application | .Infrastructure | .Functions
  building-blocks/BuildingBlocks.SharedKernel | BuildingBlocks.Contracts
  AppHost/
tests/<Servico>.Tests/
docs/adr/
```

## Testes

```powershell
node --test .claude/hooks/tests/hooks.test.js
```

## Desligar um hook

Remova a entrada dele em `.claude/settings.json`. Para desligar só na sua máquina, sem mexer no time,
use `.claude/settings.local.json` com `"disableAllHooks": true` (desliga todos).
