'use strict';
// Testes dos hooks: cada hook roda como processo separado, com o JSON de entrada no stdin,
// dentro de um projeto temporário (CLAUDE_PROJECT_DIR). Rodar: node --test .claude/hooks/tests/

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('fs');
const os = require('os');
const path = require('path');
const cp = require('child_process');

const HOOKS = path.resolve(__dirname, '..');
let counter = 0;

function makeProject() {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'tcg-hooks-test-'));
  fs.mkdirSync(path.join(root, '.claude'), { recursive: true });
  fs.copyFileSync(path.join(HOOKS, '..', 'project.json'), path.join(root, '.claude', 'project.json'));
  return root;
}
function write(root, rel, content) {
  const abs = path.join(root, rel);
  fs.mkdirSync(path.dirname(abs), { recursive: true });
  fs.writeFileSync(abs, content);
  return abs;
}
function runHook(name, root, input, env = {}) {
  const session = `test-${process.pid}-${++counter}`;
  const r = cp.spawnSync(process.execPath, [path.join(HOOKS, name)], {
    input: JSON.stringify({ session_id: session, cwd: root, ...input }),
    encoding: 'utf8',
    env: { ...process.env, CLAUDE_PROJECT_DIR: root, ...env },
    timeout: 120000,
  });
  let json = null;
  try { json = r.stdout ? JSON.parse(r.stdout) : null; } catch { json = { raw: r.stdout }; }
  return { code: r.status, json, stderr: r.stderr, session };
}
const decision = (res) => res.json?.hookSpecificOutput?.permissionDecision;
const context = (res) => res.json?.hookSpecificOutput?.additionalContext;
const writeTool = (root, rel, content) => ({ tool_name: 'Write', tool_input: { file_path: path.join(root, rel), content } });
const editTool = (root, rel, oldS, newS) => ({ tool_name: 'Edit', tool_input: { file_path: path.join(root, rel), old_string: oldS, new_string: newS } });

const root = makeProject();
test.after(() => fs.rmSync(root, { recursive: true, force: true }));

// ---------- 1. protect-files ----------
test('protect-files bloqueia .env real e libera .env.example', () => {
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, '.env', 'A=1'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, '.env.example', 'A=<valor>'))), undefined);
});
test('protect-files bloqueia bin/ obj/ e .git/', () => {
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'src/x/obj/a.cs', ''))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, '.git/config', ''))), 'deny');
});
test('protect-files bloqueia senha real e aceita emuladores e placeholders', () => {
  const real = '{ "ConnectionStrings": { "db": "Host=localhost;Username=app;Password=SuperSecreta123" } }';
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'src/local.settings.json', real))), 'deny');
  const safe = JSON.stringify({
    Values: {
      AzureWebJobsStorage: 'UseDevelopmentStorage=true',
      ServiceBus: 'Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;',
      Db: 'Host=localhost;Password=${DB_PASSWORD}',
      Jwt: { SigningKey: '<defina-em-user-secrets>' },
      TokenLifetimeMinutes: '60',
    },
  });
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'src/local.settings.json', safe))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'src/appsettings.json', '{ "Jwt": { "SigningKey": "abc123def456" } }'))), 'deny');
});
test('protect-files não confunde código C# com segredo, mas pega connection string em literal', () => {
  const code = 'namespace Identity.Application;\npublic class X { void M(User user, string password) { user.Password = password; var pwd = Hash(password); } }';
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'src/services/Identity/Identity.Application/X.cs', code))), undefined);
  const literal = 'var cs = "Host=db;Username=app;Password=Segredo123;";';
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'tests/Identity.Tests/Db.cs', literal))), 'deny');
  const interpolated = 'var cs = $"Host=db;Password={senha}";';
  assert.equal(decision(runHook('protect-files.js', root, writeTool(root, 'tests/Identity.Tests/Db2.cs', interpolated))), undefined);
});
test('protect-files limita o agent qa a tests/ e o documentador a .md fora dos ADRs', () => {
  const asAgent = (agent, rel) => ({ ...writeTool(root, rel, 'x'), agent_type: agent });
  assert.equal(decision(runHook('protect-files.js', root, asAgent('qa', 'tests/Catalog.Tests/CardTests.cs'))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, asAgent('qa', 'src/services/Catalog/Catalog.Domain/Card.cs'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, { ...writeTool(os.tmpdir(), 'x.cs', 'x'), agent_type: 'qa' })), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'docs/arquitetura.md'))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'docs/adr/README.md'))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'docs/adr/0003-outbox.md'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'docs/adr/000003~1.MD'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'src/AppHost/AppHost.cs'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', '.claude/agents/qa.md'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'CLAUDE.md'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'README.md'))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'src/services/Catalog/README.md'))), undefined);
  assert.equal(decision(runHook('protect-files.js', root, asAgent('documentador', 'notas.md'))), 'deny');
  assert.equal(decision(runHook('protect-files.js', root, asAgent('arquiteto-qualquer', 'src/services/Catalog/Catalog.Domain/Card.cs'))), undefined);
});
test('protect-files ignora arquivos fora do projeto', () => {
  assert.equal(decision(runHook('protect-files.js', root, writeTool(os.tmpdir(), '.env', 'A=1'))), undefined);
});

// ---------- 2. layer-guard ----------
test('layer-guard bloqueia EF Core no Domain', () => {
  const code = 'using Microsoft.EntityFrameworkCore;\nnamespace Catalog.Domain;\npublic class Card {}';
  const res = runHook('layer-guard.js', root, writeTool(root, 'src/services/Catalog/Catalog.Domain/Card.cs', code));
  assert.equal(decision(res), 'deny');
});
test('layer-guard bloqueia um serviço usando código de outro', () => {
  const code = 'using Inventory.Domain.Listings;\nnamespace Ordering.Application;\npublic class X {}';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Ordering/Ordering.Application/X.cs', code))), 'deny');
});
test('layer-guard bloqueia Application usando Infrastructure do mesmo serviço', () => {
  const code = 'using Catalog.Infrastructure.Persistence;\nnamespace Catalog.Application;\npublic class Y {}';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Catalog/Catalog.Application/Y.cs', code))), 'deny');
});
test('layer-guard libera dependências permitidas', () => {
  const app = 'using Catalog.Domain.Cards;\nusing BuildingBlocks.Contracts;\nnamespace Catalog.Application;\npublic class Z {}';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Catalog/Catalog.Application/Z.cs', app))), undefined);
  const infra = 'using Microsoft.EntityFrameworkCore;\nusing Catalog.Application;\nnamespace Catalog.Infrastructure;\npublic class Db {}';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Catalog/Catalog.Infrastructure/Db.cs', infra))), undefined);
});
test('layer-guard bloqueia ProjectReference entre serviços e pacote de infra no Domain', () => {
  const cross = '<Project><ItemGroup><ProjectReference Include="..\\..\\Inventory\\Inventory.Domain\\Inventory.Domain.csproj" /></ItemGroup></Project>';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Cart/Cart.Application/Cart.Application.csproj', cross))), 'deny');
  const pkg = '<Project><ItemGroup><PackageReference Include="Npgsql" /></ItemGroup></Project>';
  assert.equal(decision(runHook('layer-guard.js', root, writeTool(root, 'src/services/Cart/Cart.Domain/Cart.Domain.csproj', pkg))), 'deny');
});

// ---------- 3. dangerous-commands ----------
function gitRepo(branch) {
  const repo = fs.mkdtempSync(path.join(os.tmpdir(), 'tcg-hooks-git-'));
  const git = (...a) => cp.spawnSync('git', ['-C', repo, '-c', 'user.name=t', '-c', 'user.email=t@t', ...a], { encoding: 'utf8' });
  git('init', '-b', 'main');
  fs.writeFileSync(path.join(repo, 'a.txt'), 'a');
  git('add', '.');
  git('commit', '-m', 'init');
  if (branch !== 'main') git('switch', '-c', branch);
  return repo;
}
const bash = (command, cwd) => ({ tool_name: 'Bash', cwd, tool_input: { command } });

test('dangerous-commands bloqueia push forçado, reset --hard e clean -fd', () => {
  for (const c of ['git push --force origin feat/x', 'git push -f', 'git push origin +feat/x', 'git reset --hard HEAD~1', 'git clean -fd']) {
    assert.equal(decision(runHook('dangerous-commands.js', root, bash(c, root))), 'deny', c);
  }
});
test('dangerous-commands bloqueia commit e push na main, libera em branch', () => {
  const onMain = gitRepo('main');
  const onFeat = gitRepo('feat/hooks');
  try {
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git commit -m "x"', onMain))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push', onMain))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push origin main', onFeat))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash(`cd "${onMain}" && git commit -m x`, onFeat))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git add . && git commit -m "feat: x"', onFeat))), undefined);
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push -u origin feat/hooks', onFeat))), undefined);
    // troca de branch na mesma linha
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git switch -c feat/x && git commit -m x', onMain))), undefined);
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git checkout -b feat/y && git push -u origin HEAD', onMain))), undefined);
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git checkout main && git commit -m x', onFeat))), 'deny');
    // HEAD como refspec
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push -u origin HEAD', onMain))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push origin HEAD:main', onFeat))), 'deny');
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git push origin HEAD', onFeat))), undefined);
  } finally {
    fs.rmSync(onMain, { recursive: true, force: true });
    fs.rmSync(onFeat, { recursive: true, force: true });
  }
});
test('dangerous-commands libera o primeiro commit de um repositório novo', () => {
  const repo = fs.mkdtempSync(path.join(os.tmpdir(), 'tcg-hooks-git-'));
  cp.spawnSync('git', ['-C', repo, 'init', '-b', 'main']);
  try {
    assert.equal(decision(runHook('dangerous-commands.js', root, bash('git commit -m "chore: inicio"', repo))), undefined);
  } finally {
    fs.rmSync(repo, { recursive: true, force: true });
  }
});
test('dangerous-commands bloqueia apagar volumes e pastas raiz, libera comandos comuns', () => {
  for (const c of [
    'docker volume rm pgdata', 'docker system prune -a', 'docker compose down -v', 'rm -rf src',
    'Remove-Item -Recurse -Force .\\tests', 'rm --recursive src', 'rmdir /s /q src', 'git checkout .', 'git stash clear',
  ]) {
    assert.equal(decision(runHook('dangerous-commands.js', root, bash(c, root))), 'deny', c);
  }
  for (const c of [
    'git status', 'dotnet test', 'docker compose up -d', 'rm -rf src/services/Catalog/bin', 'git restore --staged .',
    'git checkout -- src/a.cs', 'git push --force-with-lease origin feat/x',
  ]) {
    assert.equal(decision(runHook('dangerous-commands.js', root, bash(c, root))), undefined, c);
  }
});

test('dangerous-commands limita o Bash dos agents a leitura e testes', () => {
  const asAgent = (agent, command) => ({ ...bash(command, root), agent_type: agent });
  const cases = [
    ['qa', 'dotnet test tests/Catalog.Tests', undefined],
    ['qa', 'git diff --stat && dotnet build', undefined],
    ['qa', 'dotnet test 2>&1', undefined],
    ['qa', 'echo x > src/services/Catalog/Catalog.Domain/Card.cs', 'deny'],
    ['qa', "sed -i 's/a/b/' src/a.cs", 'deny'],
    ['qa', 'Set-Content src/a.cs x', 'deny'],
    ['qa', 'git commit -m x', 'deny'],
    ['documentador', 'git log --oneline -20', undefined],
    ['documentador', 'dotnet build', 'deny'],
    ['documentador', 'cat docs/adr/0001-x.md | tee docs/adr/0001-x.md', 'deny'],
    ['arquiteto', 'git show HEAD:src/a.cs', undefined],
    ['arquiteto', 'rm src/a.cs', 'deny'],
    ['arquiteto', 'git diff $(git merge-base HEAD main)', 'deny'],
    [undefined, 'echo ok > notas.txt', undefined],
  ];
  for (const [agent, command, expected] of cases) {
    assert.equal(decision(runHook('dangerous-commands.js', root, asAgent(agent, command))), expected, `${agent}: ${command}`);
  }
});

// ---------- 4. format-cs ----------
test('format-cs formata o arquivo .cs alterado', { timeout: 90000 }, () => {
  fs.copyFileSync(path.join(HOOKS, '..', '..', '.editorconfig'), path.join(root, '.editorconfig'));
  const rel = 'src/services/Catalog/Catalog.Domain/Fmt.cs';
  const abs = write(root, rel, 'namespace Catalog.Domain;\npublic class Fmt\n{\n      public int  A { get; set; }\n}\n');
  runHook('format-cs.js', root, { tool_name: 'Edit', tool_input: { file_path: abs } });
  assert.match(fs.readFileSync(abs, 'utf8'), /\n {4}public int A \{ get; set; \}/);
});

// ---------- 5. tdd-reminder ----------
test('tdd-reminder avisa quando falta teste e fica quieto quando existe', () => {
  const code = 'namespace Inventory.Domain.Listings;\npublic class Listing {}';
  const missing = runHook('tdd-reminder.js', root, writeTool(root, 'src/services/Inventory/Inventory.Domain/Listings/Listing.cs', code));
  assert.match(context(missing) || '', /tests\/Inventory\.Tests\/Domain\/Listings\/ListingTests\.cs/);
  write(root, 'tests/Inventory.Tests/Domain/Listings/ListingTests.cs', '');
  assert.equal(runHook('tdd-reminder.js', root, writeTool(root, 'src/services/Inventory/Inventory.Domain/Listings/Listing.cs', code)).json, null);
  const iface = 'namespace Inventory.Domain;\npublic interface IListingRepository {}';
  assert.equal(runHook('tdd-reminder.js', root, writeTool(root, 'src/services/Inventory/Inventory.Domain/IListingRepository.cs', iface)).json, null);
});

// ---------- 6. build-test-on-stop ----------
test('build-test-on-stop não faz nada sem alterações registradas', () => {
  assert.equal(runHook('build-test-on-stop.js', root, { hook_event_name: 'Stop' }).json, null);
});
test('build-test-on-stop bloqueia quando o build falha e libera quando passa', { timeout: 300000 }, () => {
  const proj = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable></PropertyGroup></Project>';
  write(root, 'src/services/Store/Store.Domain/Store.Domain.csproj', proj);
  const cls = write(root, 'src/services/Store/Store.Domain/Shop.cs', 'namespace Store.Domain;\npublic class Shop { public int X => "erro"; }\n');
  const session = `build-${process.pid}`;
  const track = () => cp.spawnSync(process.execPath, [path.join(HOOKS, 'track-changes.js')], {
    input: JSON.stringify({ session_id: session, cwd: root, tool_name: 'Write', tool_input: { file_path: cls } }),
    env: { ...process.env, CLAUDE_PROJECT_DIR: root }, encoding: 'utf8',
  });
  const stop = () => {
    const r = cp.spawnSync(process.execPath, [path.join(HOOKS, 'build-test-on-stop.js')], {
      input: JSON.stringify({ session_id: session, cwd: root, hook_event_name: 'Stop' }),
      env: { ...process.env, CLAUDE_PROJECT_DIR: root }, encoding: 'utf8', timeout: 240000,
    });
    return JSON.parse(r.stdout || 'null');
  };
  track();
  const failed = stop();
  assert.equal(failed.decision, 'block');
  assert.match(failed.reason, /Store\.Domain/);
  const result = JSON.parse(fs.readFileSync(path.join(os.tmpdir(), 'tcg-marketplace-hooks', `${session}.stop-result.json`), 'utf8'));
  assert.equal(result.status, 'blocked');
  fs.writeFileSync(cls, 'namespace Store.Domain;\npublic class Shop { public int X => 1; }\n');
  const ok = stop();
  assert.match(ok.systemMessage, /OK: build Store\.Domain/);
  assert.equal(stop(), null, 'depois de passar, a lista de alterações é limpa');
});

// ---------- 7. adr-reminder ----------
test('adr-reminder avisa uma vez por tipo de mudança', () => {
  const csproj = '<Project><ItemGroup><PackageReference Include="Npgsql" /></ItemGroup></Project>';
  const res = runHook('adr-reminder.js', root, writeTool(root, 'src/services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj', csproj));
  assert.match(context(res) || '', /docs\/adr/);
  const plain = runHook('adr-reminder.js', root, writeTool(root, 'src/services/Catalog/Catalog.Domain/Card.cs', 'class Card {}'));
  assert.equal(plain.json, null);
});

// ---------- 8. contracts-guard ----------
test('contracts-guard pede confirmação ao remover campo e libera ao adicionar', () => {
  const rel = 'src/building-blocks/BuildingBlocks.Contracts/Inventory/StockReserved.cs';
  write(root, rel, 'namespace BuildingBlocks.Contracts.Inventory;\npublic record StockReserved(Guid OrderId, Guid ListingId, int Quantity);\n');
  const removed = editTool(root, rel, 'public record StockReserved(Guid OrderId, Guid ListingId, int Quantity);', 'public record StockReserved(Guid OrderId, int Quantity);');
  assert.equal(decision(runHook('contracts-guard.js', root, removed)), 'ask');
  const added = editTool(root, rel, 'public record StockReserved(Guid OrderId, Guid ListingId, int Quantity);', 'public record StockReserved(Guid OrderId, Guid ListingId, int Quantity, DateTime ExpiresAt);');
  assert.equal(decision(runHook('contracts-guard.js', root, added)), undefined);
  assert.equal(decision(runHook('contracts-guard.js', root, writeTool(root, 'src/building-blocks/BuildingBlocks.Contracts/Novo.cs', 'public record Novo(int A);'))), undefined);
});
test('contracts-guard entende atributos com parênteses e ignora comentários', () => {
  const rel = 'src/building-blocks/BuildingBlocks.Contracts/Ordering/OrderCreated.cs';
  const before = '// class Antigo está só no comentário\npublic record OrderCreated([property: JsonPropertyName("order_id")] Guid OrderId, decimal Total);';
  write(root, rel, before);
  const addField = before.replace('decimal Total)', 'decimal Total, string Currency)');
  assert.equal(decision(runHook('contracts-guard.js', root, writeTool(root, rel, addField))), undefined);
  const dropTotal = before.replace(', decimal Total)', ')');
  assert.equal(decision(runHook('contracts-guard.js', root, writeTool(root, rel, dropTotal))), 'ask');
});

// ---------- 9. session-context ----------
test('session-context informa fase e ferramentas', { timeout: 60000 }, () => {
  const text = context(runHook('session-context.js', root, { hook_event_name: 'SessionStart' })) || '';
  assert.match(text, /Fase atual: Fase \d/);
  assert.match(text, /dotnet \d+\.\d+/);
  assert.match(text, /docker/);
});

// ---------- 10. notify ----------
test('notify avisa em Notification e só em tarefas longas no Stop', () => {
  const env = { TCG_HOOKS_NOTIFY_DRY: '1', TCG_HOOKS_NOTIFY_DELAY_MS: '0' };
  const n = runHook('notify.js', root, { hook_event_name: 'Notification', message: 'Claude precisa de permissão' }, env);
  assert.equal(n.json.toast, 'Claude precisa de permissão');

  const session = `notify-${process.pid}`;
  const stateDir = path.join(os.tmpdir(), 'tcg-marketplace-hooks');
  fs.mkdirSync(stateDir, { recursive: true });
  const stopWith = (secondsAgo) => {
    fs.writeFileSync(path.join(stateDir, `${session}.prompt.json`), JSON.stringify({ t: Date.now() - secondsAgo * 1000 }));
    const r = cp.spawnSync(process.execPath, [path.join(HOOKS, 'notify.js')], {
      input: JSON.stringify({ session_id: session, hook_event_name: 'Stop' }),
      env: { ...process.env, ...env }, encoding: 'utf8',
    });
    return r.stdout ? JSON.parse(r.stdout) : null;
  };
  assert.equal(stopWith(10), null);
  assert.match(stopWith(180).toast, /3 min/);
  assert.match(stopWith(180).toast, /3 min/, 'um pedido novo volta a permitir o aviso');

  // build-test devolveu o turno ao Claude: ainda não terminou, nada de aviso
  fs.writeFileSync(path.join(stateDir, `${session}.prompt.json`), JSON.stringify({ t: Date.now() - 300000 }));
  fs.writeFileSync(path.join(stateDir, `${session}.stop-result.json`), JSON.stringify({ status: 'blocked', t: Date.now() }));
  const r = cp.spawnSync(process.execPath, [path.join(HOOKS, 'notify.js')], {
    input: JSON.stringify({ session_id: session, hook_event_name: 'Stop' }),
    env: { ...process.env, ...env }, encoding: 'utf8',
  });
  assert.equal(r.stdout, '');
});
