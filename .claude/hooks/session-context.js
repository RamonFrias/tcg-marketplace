'use strict';
// Hook 9 · SessionStart
// Passa ao Claude o estado do projeto: fase atual, branch, alterações sem commit e ferramentas disponíveis.

const cp = require('child_process');
const L = require('./lib');

const TOOLS = ['dotnet', 'func', 'docker', 'node'];

function version(cmd) {
  return new Promise((resolve) => {
    cp.exec(`${cmd} --version`, { timeout: 8000, windowsHide: true }, (err, stdout) => {
      if (err) return resolve(null);
      resolve(String(stdout).trim().split(/\r?\n/)[0].replace(/^Docker version\s*/i, '').replace(/,.*$/, ''));
    });
  });
}

function gitLines(root) {
  const inside = L.run('git', ['-C', root, 'rev-parse', '--is-inside-work-tree'], { timeout: 5000 });
  if (!inside.ok) return ['- Git: a pasta ainda não é um repositório git.'];
  const branch = L.run('git', ['-C', root, 'symbolic-ref', '--short', '-q', 'HEAD'], { timeout: 5000 });
  const status = L.run('git', ['-C', root, 'status', '--porcelain'], { timeout: 8000 });
  const pending = status.ok ? status.out.split(/\r?\n/).filter(Boolean).length : 0;
  const changes = pending ? `${pending} arquivo(s) alterado(s) sem commit` : 'sem alterações pendentes';
  if (!branch.ok) return [`- Git: HEAD destacado (detached), fora de qualquer branch (${changes}).`];
  const name = branch.out.trim();
  const lines = [`- Branch: ${name} (${changes})`];
  if (name === 'main' || name === 'master') {
    lines.push('  Fluxo trunk-based: crie uma branch antes de commitar (git switch -c feat/<nome>).');
  }
  return lines;
}

L.main(async (evt) => {
  const root = L.projectRoot(evt);
  const cfg = L.projectConfig(evt);
  const versionsPromise = Promise.all(TOOLS.map(async (cmd) => ({ cmd, v: await version(cmd) })));

  const lines = ['[tcg-marketplace] Contexto da sessão'];
  if (cfg.faseAtual) {
    lines.push(`- Fase atual: ${cfg.faseAtual}${cfg.proximaFase ? ` (próxima: ${cfg.proximaFase})` : ''}`);
  }
  lines.push(...gitLines(root));

  const versions = await versionsPromise;
  lines.push(`- Ferramentas: ${versions.map(({ cmd, v }) => `${cmd} ${v || 'NÃO ENCONTRADO'}`).join(' · ')}`);
  if (!versions.find((x) => x.cmd === 'docker').v) {
    lines.push(
      '  Docker ausente: o Aspire não consegue subir Postgres, Service Bus Emulator, Azurite e OpenSearch. ' +
      'Avise o usuário antes de qualquer tarefa que precise deles.'
    );
  }
  if (cfg.decisoes) lines.push(`- Decisões de arquitetura registradas em: ${cfg.decisoes}`);

  L.addContext('SessionStart', lines.join('\n'));
});
