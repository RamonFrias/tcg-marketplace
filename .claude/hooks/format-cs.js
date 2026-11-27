'use strict';
// Hook 4 · PostToolUse (Write|Edit|MultiEdit)
// Formata só o .cs alterado com `dotnet format whitespace` (modo pasta: não precisa de restore/build),
// seguindo o .editorconfig da raiz do repositório.

const L = require('./lib');

L.main((evt) => {
  const rel = L.relPath(evt, L.filePath(evt));
  if (!rel || !rel.endsWith('.cs') || /(^|\/)(bin|obj)\//.test(rel)) return;

  const root = L.projectRoot(evt);
  const r = L.run('dotnet', ['format', 'whitespace', '.', '--folder', '--include', rel, '--verbosity', 'quiet'], {
    cwd: root,
    timeout: 35000,
  });
  if (r.error && r.error.code === 'ENOENT') {
    L.emit({ systemMessage: `[format-cs] dotnet não encontrado no PATH: ${rel} não foi formatado.` });
    return;
  }
  if (!r.ok) {
    const why = r.error ? `${r.error.code || r.error.message} ` : '';
    L.emit({ systemMessage: `[format-cs] dotnet format falhou em ${rel}: ${why}${r.out.trim().slice(0, 300)}` });
  }
});
