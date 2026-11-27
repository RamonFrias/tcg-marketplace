'use strict';
// Hook 6 · Stop
// Ao fim do turno, compila e testa só os serviços cujo código o Claude alterou nesta sessão.
// Se falhar, devolve os erros e o Claude continua corrigindo (até 3 vezes seguidas; depois só avisa).
// Grava o resultado em "stop-result" (running/blocked/done) para o notify.js saber se o turno acabou de fato.

const fs = require('fs');
const path = require('path');
const L = require('./lib');

const MAX_BLOCKS = 3;
const BUDGET_MS = 540000; // o hook tem 600s no settings.json; sobra margem para responder
const STEP_TIMEOUT_MS = 240000;
const MIN_STEP_MS = 15000;

function nearestProject(root, rel) {
  let dir = path.dirname(path.join(root, rel));
  while (dir.startsWith(root) && dir !== root) {
    let entries = [];
    try { entries = fs.readdirSync(dir); } catch { /* pasta removida */ }
    const proj = entries.find((f) => f.endsWith('.csproj'));
    if (proj) return path.join(dir, proj);
    dir = path.dirname(dir);
  }
  return null;
}

function solutionFile(root) {
  try {
    const sln = fs.readdirSync(root).find((f) => /\.slnx?$/.test(f));
    return sln ? path.join(root, sln) : null;
  } catch {
    return null;
  }
}

function summarize(output) {
  const all = output.split(/\r?\n/).filter((l) => l.trim());
  const relevant = all.filter((l) => /\berror\b|\bfailed\b|falh|erro|Assert|Expected|Actual|Esperado|Atual/i.test(l));
  return [...new Set((relevant.length ? relevant : all).slice(-60))].join('\n');
}

function plan(evt, root) {
  const changed = L.readState(evt, 'changed', []);
  const services = L.services(evt);
  const touched = new Set();
  const builds = new Set();

  for (const rel of changed) {
    const svc = rel.match(/^src\/services\/([^/]+)\//);
    const test = rel.match(/^tests\/([^/.]+)\./);
    if (svc) touched.add(svc[1]);
    else if (test && services.includes(test[1])) touched.add(test[1]);
    else if (/^src\/building-blocks\//.test(rel)) {
      // biblioteca compartilhada: compila a solution inteira uma vez (pega quebras nos consumidores)
      const target = solutionFile(root) || nearestProject(root, rel);
      if (target) builds.add(target);
    } else {
      const proj = nearestProject(root, rel);
      if (proj) builds.add(proj);
    }
  }

  const tests = new Set();
  const solution = [...builds].find((b) => /\.slnx?$/.test(b));
  for (const svc of touched) {
    if (!solution) L.walk(path.join(root, 'src', 'services', svc), (n) => n.endsWith('.csproj')).forEach((p) => builds.add(p));
    L.walk(path.join(root, 'tests'), (n, p) => n.endsWith('.csproj') && path.basename(path.dirname(p)).startsWith(`${svc}.`))
      .forEach((p) => tests.add(p));
  }
  for (const p of [...builds]) {
    if (L.toPosix(path.relative(root, p)).startsWith('tests/')) { builds.delete(p); tests.add(p); }
  }
  return { changed, steps: [...[...builds].map((p) => ['build', p]), ...[...tests].map((p) => ['test', p])] };
}

function check(evt) {
  const root = L.projectRoot(evt);
  const { changed, steps } = plan(evt, root);
  if (!changed.length) return 'done';
  if (!steps.length) { L.clearState(evt, 'changed'); return 'done'; }

  const started = Date.now();
  const failures = [];
  const passed = [];
  const skipped = [];
  for (const [kind, proj] of steps) {
    const name = path.basename(proj).replace(/\.(csproj|slnx?)$/, '');
    const remaining = BUDGET_MS - (Date.now() - started);
    if (remaining < MIN_STEP_MS) { skipped.push(`${kind} ${name}`); continue; }

    const args = kind === 'build'
      ? ['build', proj, '--nologo', '-v', 'q', '-clp:ErrorsOnly']
      : ['test', '--project', proj, '--no-progress']; // Microsoft.Testing.Platform (global.json): não aceita --nologo
    const r = L.run('dotnet', args, { cwd: root, timeout: Math.min(STEP_TIMEOUT_MS, remaining) });
    if (r.error && r.error.code === 'ENOENT') {
      L.emit({ systemMessage: '[build-test] dotnet não encontrado no PATH: build e testes não rodaram.' });
      return 'done';
    }
    if (r.error && r.error.code === 'ETIMEDOUT') { skipped.push(`${kind} ${name} (tempo esgotado)`); continue; }
    if (r.ok) { passed.push(`${kind} ${name}`); continue; }
    const detail = r.error ? `(${r.error.code || r.error.message}) ` : '';
    failures.push(`### dotnet ${kind} ${L.toPosix(path.relative(root, proj))}\n${detail}${summarize(r.out) || 'sem saída'}`);
  }

  if (!failures.length) {
    L.clearState(evt, 'changed');
    L.clearState(evt, 'stopblocks');
    const note = skipped.length ? ` · não rodou por falta de tempo: ${skipped.join(', ')}` : '';
    L.emit({ systemMessage: `[build-test] OK: ${passed.join(', ') || 'nada'}${note}` });
    return 'done';
  }

  const notRun = skipped.length ? `\n\nNão rodaram por falta de tempo: ${skipped.join(', ')}.` : '';
  const blocks = L.readState(evt, 'stopblocks', 0);
  if (blocks >= MAX_BLOCKS) {
    L.clearState(evt, 'stopblocks');
    L.emit({
      systemMessage:
        `[build-test] Ainda há falhas depois de ${MAX_BLOCKS} tentativas de correção:\n` +
        `${failures.join('\n\n').slice(0, 1500)}${notRun}`,
    });
    return 'done';
  }
  L.writeState(evt, 'stopblocks', blocks + 1);
  L.emit({
    decision: 'block',
    reason:
      `[build-test] Build ou testes dos serviços alterados falharam (tentativa ${blocks + 1}/${MAX_BLOCKS}). ` +
      `Corrija antes de encerrar:\n\n${failures.join('\n\n').slice(0, 6000)}${notRun}`,
  });
  return 'blocked';
}

L.main((evt) => {
  L.writeState(evt, 'stop-result', { status: 'running', t: Date.now() });
  let status = 'done';
  try {
    status = check(evt);
  } finally {
    // O resultado só serve ao aviso do notify.js. Se a gravação falhar, avisa no stderr sem trocar o
    // exit code: a decisão (block) já foi emitida e o Claude Code só a lê com exit 0.
    try {
      L.writeState(evt, 'stop-result', { status, t: Date.now() });
    } catch (e) {
      process.stderr.write(`[build-test-on-stop] não gravou o resultado para o notify: ${e.message}\n`);
    }
  }
});
