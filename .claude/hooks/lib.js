'use strict';
// Funções comuns dos hooks do tcg-marketplace.
// Nenhuma falha é silenciosa:
//   - violação de regra (deny)            → exit 2 + motivo no stderr: a ação é bloqueada.
//   - erro interno num hook de guarda     → exit 2 (fail-closed): na dúvida, bloqueia.
//   - erro interno num hook de aviso      → exit 1 + mensagem no stderr: aparece como erro, não bloqueia.
// Hooks de guarda chamam main(fn, { guard: true }).

const fs = require('fs');
const os = require('os');
const path = require('path');
const cp = require('child_process');

const DEFAULT_SERVICES = [
  'Catalog', 'Store', 'Inventory', 'Search', 'Cart',
  'Ordering', 'Payment', 'Shipping', 'Notification', 'Identity',
];
const SKIP_DIRS = new Set(['bin', 'obj', '.git', '.vs', '.idea', 'node_modules', 'TestResults']);

function readInput() {
  return new Promise((resolve, reject) => {
    let raw = '';
    process.stdin.setEncoding('utf8');
    process.stdin.on('data', (c) => { raw += c; });
    process.stdin.on('end', () => {
      try {
        const evt = JSON.parse(raw);
        if (!evt || typeof evt !== 'object') throw new Error('o JSON de entrada não é um objeto');
        resolve(evt);
      } catch (e) {
        reject(new Error(`entrada do hook inválida (${e.message})`));
      }
    });
    process.stdin.on('error', reject);
  });
}

const scriptName = () => path.basename(process.argv[1] || 'hook');

function main(fn, { guard = false } = {}) {
  readInput()
    .then((evt) => fn(evt))
    .catch((e) => {
      const effect = guard
        ? 'Ação bloqueada por segurança: o hook de guarda não conseguiu verificar a regra.'
        : 'A ação seguiu, mas o hook não rodou.';
      process.stderr.write(`[${scriptName()}] falha interna: ${e && e.message ? e.message : e}. ${effect}\n`);
      if (process.env.TCG_HOOKS_DEBUG && e && e.stack) process.stderr.write(`${e.stack}\n`);
      process.exitCode = guard ? 2 : 1;
    });
}

const toPosix = (p) => String(p || '').replace(/\\/g, '/');

function projectRoot(evt) {
  return path.resolve(process.env.CLAUDE_PROJECT_DIR || (evt && evt.cwd) || process.cwd());
}

// Caminho relativo à raiz do projeto, com "/". null quando o arquivo está fora do projeto.
function relPath(evt, file) {
  if (!file) return null;
  const root = projectRoot(evt);
  const rel = path.relative(root, path.resolve(root, file));
  if (!rel || rel.startsWith('..') || path.isAbsolute(rel)) return null;
  return toPosix(rel);
}

function filePath(evt) {
  const i = (evt && evt.tool_input) || {};
  return i.file_path || i.notebook_path || null;
}

// Texto que a ferramenta vai gravar (Write: arquivo inteiro; Edit/MultiEdit: só os trechos novos).
function newContent(evt) {
  const i = (evt && evt.tool_input) || {};
  if (typeof i.content === 'string') return i.content;
  if (typeof i.new_string === 'string') return i.new_string;
  if (Array.isArray(i.edits)) return i.edits.map((e) => e.new_string || '').join('\n');
  if (typeof i.new_source === 'string') return i.new_source;
  return '';
}

// Texto que está sendo substituído (Edit: old_string; Write: arquivo atual em disco).
function oldContent(evt, absFile) {
  const i = (evt && evt.tool_input) || {};
  if (typeof i.old_string === 'string') return i.old_string;
  if (Array.isArray(i.edits)) return i.edits.map((e) => e.old_string || '').join('\n');
  if (typeof i.content === 'string') {
    try { return fs.readFileSync(absFile, 'utf8'); } catch { return ''; }
  }
  return '';
}

function emit(obj) { process.stdout.write(JSON.stringify(obj)); }

function preToolDecision(decision, reason) {
  emit({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: decision,
      permissionDecisionReason: reason,
    },
  });
}
// Bloqueio por exit 2 + stderr: a forma mais garantida de barrar a ferramenta (o motivo volta para o Claude).
function deny(reason) {
  process.stderr.write(`${reason}\n`);
  process.exitCode = 2;
}
const ask = (reason) => preToolDecision('ask', reason);

function addContext(eventName, text) {
  emit({ hookSpecificOutput: { hookEventName: eventName, additionalContext: text } });
}

function projectConfig(evt) {
  try {
    return JSON.parse(fs.readFileSync(path.join(projectRoot(evt), '.claude', 'project.json'), 'utf8'));
  } catch {
    return {};
  }
}

// Serviços conhecidos: os do project.json + as pastas que existirem em src/services.
function services(evt) {
  const set = new Set(projectConfig(evt).servicos || DEFAULT_SERVICES);
  try {
    for (const d of fs.readdirSync(path.join(projectRoot(evt), 'src', 'services'), { withFileTypes: true })) {
      if (d.isDirectory()) set.add(d.name);
    }
  } catch { /* pasta ainda não existe */ }
  return [...set];
}

// Estado por sessão, guardado na pasta temporária do sistema (nada vai para o repositório).
function stateFile(evt, name) {
  const dir = path.join(os.tmpdir(), 'tcg-marketplace-hooks');
  fs.mkdirSync(dir, { recursive: true });
  const sid = String((evt && evt.session_id) || 'sem-sessao').replace(/[^\w-]/g, '_');
  return path.join(dir, `${sid}.${name}.json`);
}
function readState(evt, name, fallback) {
  try { return JSON.parse(fs.readFileSync(stateFile(evt, name), 'utf8')); } catch { return fallback; }
}
function writeState(evt, name, value) {
  fs.writeFileSync(stateFile(evt, name), JSON.stringify(value));
}
function clearState(evt, name) {
  try {
    fs.unlinkSync(stateFile(evt, name));
  } catch (e) {
    if (e.code !== 'ENOENT') throw e; // só "já não existe" é esperado
  }
}
// true só na primeira vez que a chave aparece na sessão (evita repetir o mesmo aviso).
function onceInSession(evt, key) {
  const seen = readState(evt, 'once', {});
  if (seen[key]) return false;
  seen[key] = 1;
  writeState(evt, 'once', seen);
  return true;
}

function run(cmd, args = [], opts = {}) {
  const base = {
    encoding: 'utf8', windowsHide: true, timeout: opts.timeout || 20000, cwd: opts.cwd,
    maxBuffer: 16 * 1024 * 1024, env: opts.env ? { ...process.env, ...opts.env } : undefined,
  };
  const r = opts.shell
    ? cp.spawnSync([cmd, ...args].join(' '), { ...base, shell: true })
    : cp.spawnSync(cmd, args, base);
  return { ok: r.status === 0, code: r.status, out: `${r.stdout || ''}${r.stderr || ''}`, error: r.error };
}

function walk(dir, predicate, acc = [], depth = 0) {
  if (depth > 10) return acc;
  let entries;
  try { entries = fs.readdirSync(dir, { withFileTypes: true }); } catch { return acc; }
  for (const e of entries) {
    if (SKIP_DIRS.has(e.name)) continue;
    const p = path.join(dir, e.name);
    if (e.isDirectory()) walk(p, predicate, acc, depth + 1);
    else if (predicate(e.name, p)) acc.push(p);
  }
  return acc;
}

module.exports = {
  DEFAULT_SERVICES, main, toPosix, projectRoot, relPath, filePath, newContent, oldContent,
  emit, deny, ask, addContext, projectConfig, services,
  readState, writeState, clearState, onceInSession, run, walk,
};
