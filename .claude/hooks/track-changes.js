'use strict';
// Apoio ao hook 6 · PostToolUse (Write|Edit|MultiEdit)
// Anota quais arquivos de código o Claude alterou nesta sessão, para o build-test-on-stop
// compilar e testar só os serviços afetados.

const L = require('./lib');

L.main((evt) => {
  const rel = L.relPath(evt, L.filePath(evt));
  if (!rel || !/^(src|tests)\//.test(rel) || /(^|\/)(bin|obj)\//.test(rel)) return;
  if (!/\.(cs|csproj|props|targets|json)$/.test(rel)) return;

  const changed = L.readState(evt, 'changed', []);
  if (!changed.includes(rel)) {
    changed.push(rel);
    L.writeState(evt, 'changed', changed);
  }
});
