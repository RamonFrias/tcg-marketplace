'use strict';
// Hook 3 · PreToolUse (Bash|PowerShell)
// Bloqueia comandos que reescrevem histórico, furam o fluxo trunk-based (commit/push direto na main)
// ou apagam dados locais (volumes do Docker, pastas raiz do projeto).
// Permitidos de propósito: git push --force-with-lease (seguro na própria branch) e git rebase.

const path = require('path');
const L = require('./lib');

const PROTECTED = new Set(['main', 'master']);
const CREATE_FLAGS = new Set(['-c', '-C', '-b', '-B', '--create', '--force-create']);

function tokenize(segment) {
  const out = [];
  const re = /"([^"]*)"|'([^']*)'|(\S+)/g;
  let m;
  while ((m = re.exec(segment))) out.push(m[1] ?? m[2] ?? m[3]);
  return out;
}

const isShortFlagWith = (arg, letter) => /^-[a-zA-Z]+$/.test(arg) && arg.includes(letter);

function gitInfo(dir) {
  const branch = L.run('git', ['-C', dir, 'symbolic-ref', '--short', '-q', 'HEAD'], { timeout: 5000 });
  const head = L.run('git', ['-C', dir, 'rev-parse', '--verify', '-q', 'HEAD'], { timeout: 5000 });
  return { branch: branch.ok ? branch.out.trim() : null, hasHead: head.ok };
}

// ctx.branch guarda a branch para a qual um comando anterior da mesma linha trocou
// (ex.: `git switch -c feat/x && git commit`), já que o hook roda antes de tudo.
function checkGit(tokens, cwd, ctx) {
  let i = 1;
  let dir = cwd;
  while (i < tokens.length && tokens[i].startsWith('-')) {
    if (tokens[i] === '-C') { dir = path.resolve(cwd, tokens[i + 1] || '.'); i += 2; }
    else if (tokens[i] === '-c') i += 2;
    else i += 1;
  }
  const sub = tokens[i];
  const args = tokens.slice(i + 1);
  const has = (...flags) => args.some((a) => flags.includes(a));
  const currentBranch = () => (ctx.branch !== undefined ? ctx.branch : gitInfo(dir).branch);

  if (sub === 'switch' || sub === 'checkout') {
    const createAt = args.findIndex((a) => CREATE_FLAGS.has(a));
    const dashDash = args.indexOf('--');
    const files = dashDash >= 0 ? args.slice(dashDash + 1) : args.filter((a) => !a.startsWith('-'));
    if (sub === 'checkout' && createAt < 0 && files.some((a) => a === '.' || a === '*')) {
      return 'esse comando descarta todas as alterações locais sem volta.';
    }
    if (dashDash >= 0 && createAt < 0) return null; // restaura arquivos específicos, não troca de branch
    const target = createAt >= 0 ? args[createAt + 1] : args.find((a) => !a.startsWith('-'));
    if (target) ctx.branch = target;
    return null;
  }

  if (sub === 'push') {
    if (has('--force', '--force-if-includes') || args.some((a) => isShortFlagWith(a, 'f')) || args.some((a) => a.startsWith('+'))) {
      return 'git push forçado reescreve o histórico remoto. Use --force-with-lease na sua branch ou peça ao usuário para rodar no terminal dele.';
    }
    const refspecs = args.filter((a) => !a.startsWith('-')).slice(1);
    const dest = (r) => {
      const d = (r.includes(':') ? r.split(':').pop() : r).replace(/^refs\/heads\//, '');
      return d === 'HEAD' || d === '@' ? currentBranch() : d;
    };
    const targetsMain = refspecs.length ? refspecs.some((r) => PROTECTED.has(dest(r))) : PROTECTED.has(currentBranch());
    if (targetsMain) {
      return 'push direto na main não é permitido (trunk-based com PR). Crie uma branch, faça push dela e abra um PR. ' +
        'Se for o primeiro push do repositório, peça ao usuário para rodar no terminal dele.';
    }
  }
  if (sub === 'reset' && has('--hard')) {
    return 'git reset --hard descarta alterações sem volta. Use git stash ou peça ao usuário.';
  }
  if (sub === 'clean' && (has('--force') || args.some((a) => isShortFlagWith(a, 'f')))) {
    return 'git clean -f apaga arquivos não versionados sem volta.';
  }
  if (sub === 'restore' && !has('--staged', '-S') && args.some((a) => a === '.' || a === '*')) {
    return 'esse comando descarta todas as alterações locais sem volta.';
  }
  if (sub === 'stash' && args[0] === 'clear') {
    return 'git stash clear apaga todos os stashes sem volta.';
  }
  if (sub === 'commit') {
    const { hasHead } = gitInfo(dir);
    const branch = currentBranch();
    if (hasHead && PROTECTED.has(branch)) {
      return `commit direto na ${branch} não é permitido (trunk-based). Crie uma branch antes: git switch -c feat/<nome>.`;
    }
  }
  if (sub === 'branch' && has('-D', '-d', '--delete') && args.some((a) => PROTECTED.has(a))) {
    return 'a branch main não pode ser apagada.';
  }
  return null;
}

function checkDocker(tokens) {
  const words = tokens.slice(1).filter((t) => !t.startsWith('-'));
  const flags = tokens.slice(1).filter((t) => t.startsWith('-'));
  if (words[0] === 'system' && words[1] === 'prune') {
    return 'docker system prune apaga imagens, containers e (com --volumes) os dados locais do Postgres, OpenSearch e Azurite.';
  }
  if (words[0] === 'volume' && ['rm', 'remove', 'prune'].includes(words[1])) {
    return 'remover volumes apaga os dados locais (Postgres, OpenSearch, Azurite).';
  }
  if (words[0] === 'compose' && words.includes('down') && flags.some((f) => f === '-v' || f === '--volumes')) {
    return 'docker compose down -v apaga os volumes com os dados locais.';
  }
  return null;
}

const ROOT_TARGETS = new Set(['.', '..', '/', '*', '~', 'src', 'tests', 'docs', '.git', '.claude']);

function checkRecursiveDelete(tokens) {
  const cmd = tokens[0].toLowerCase();
  const recursiveRm = cmd === 'rm' && tokens.some((t) => /^-[a-zA-Z]*r/i.test(t) || t === '--recursive');
  const recursiveWin = ['remove-item', 'ri', 'rmdir', 'rd', 'del', 'erase'].includes(cmd) &&
    tokens.some((t) => /^-r(ecurse)?$/i.test(t) || /^\/s$/i.test(t));
  if (!recursiveRm && !recursiveWin) return null;
  const targets = tokens.slice(1).filter((t) => !t.startsWith('-') && !/^\/[a-z]$/i.test(t));
  const hit = targets.find((t) => ROOT_TARGETS.has(t.replace(/^\.[\\/]/, '').replace(/[\\/]+$/, '') || t));
  return hit ? `remoção recursiva de "${hit}", uma pasta raiz do projeto.` : null;
}

// Agents do projeto com Bash só podem rodar estes comandos (um por segmento da linha).
// Sem isso, `echo > arquivo` ou `sed -i` furariam o limite de escrita do protect-files.
const GIT_READ = /^git\s+(log|diff|show|status|ls-files|blame|rev-parse|branch\s+--show-current)\b/;
const AGENT_COMMANDS = {
  arquiteto: [GIT_READ, /^dotnet\s+(build|test|restore)\b/],
  qa: [GIT_READ, /^dotnet\s+(build|test|restore)\b/],
  documentador: [GIT_READ],
};

function checkAgentCommand(agent, command) {
  const allowed = AGENT_COMMANDS[agent];
  if (!allowed) return null;
  const text = String(command);
  const withoutStderr = text.replace(/\d?>&\d|2>\s*(\$null|\/dev\/null|nul)/gi, '');
  if (/[<>`]|\$\(/.test(withoutStderr)) {
    return `o agent ${agent} não pode usar redirecionamento (>, <) nem substituição de comando.`;
  }
  for (const raw of text.split(/&&|\|\||;|\||\r?\n/)) {
    const segment = raw.trim();
    if (!segment) continue;
    if (!allowed.some((re) => re.test(segment))) {
      return `o agent ${agent} só pode rodar comandos de leitura${agent === 'documentador' ? ' do git' : ' do git e dotnet build/test'}. Recusado: \`${segment}\`.`;
    }
  }
  return null;
}

L.main((evt) => {
  const command = (evt.tool_input || {}).command;
  if (!command) return;

  const agentReason = checkAgentCommand(evt.agent_type, command);
  if (agentReason) return L.deny(`[dangerous-commands] ${agentReason}`);
  let dir = path.resolve(evt.cwd || L.projectRoot(evt));
  const ctx = { branch: undefined };

  for (const raw of String(command).split(/&&|\|\||;|\||\r?\n/)) {
    const segment = raw.trim();
    const tokens = tokenize(segment);
    if (!tokens.length) continue;
    const head = tokens[0].toLowerCase();

    if (['cd', 'set-location', 'sl', 'pushd'].includes(head)) {
      if (tokens[1]) { dir = path.resolve(dir, tokens[1]); ctx.branch = undefined; }
      continue;
    }
    let reason = null;
    if (head === 'git' || head === 'git.exe') reason = checkGit(tokens, dir, ctx);
    else if (head === 'docker' || head === 'docker.exe') reason = checkDocker(tokens);
    else reason = checkRecursiveDelete(tokens);

    if (reason) return L.deny(`[dangerous-commands] Bloqueado: \`${segment}\`. Motivo: ${reason}`);
  }
});
