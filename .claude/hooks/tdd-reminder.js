'use strict';
// Hook 5 · PostToolUse (Write)
// Quando nasce uma classe/record/struct em <Servico>.Domain ou <Servico>.Application sem o teste
// correspondente em tests/ (<Classe>Tests.cs), lembra o Claude de escrever o teste (TDD). Não bloqueia.

const path = require('path');
const L = require('./lib');

const IGNORED = /^(GlobalUsings|AssemblyInfo|DependencyInjection|ServiceCollectionExtensions|AssemblyReference)$/;

L.main((evt) => {
  if (evt.tool_name !== 'Write') return;
  const rel = L.relPath(evt, L.filePath(evt));
  if (!rel) return;
  const m = rel.match(/^src\/services\/([^/]+)\/\1\.(Domain|Application)\/(.+)\.cs$/);
  if (!m) return;

  const [, svc, layer, sub] = m;
  const cls = path.posix.basename(sub);
  const code = L.newContent(evt);
  if (IGNORED.test(cls)) return;
  if (!/\b(class|record|struct)\s+[A-Z]\w*/.test(code)) return; // só interface ou enum

  const wanted = new Set([`${cls}Tests.cs`, `${cls}Test.cs`, `${cls}Specs.cs`]);
  const found = L.walk(path.join(L.projectRoot(evt), 'tests'), (name) => wanted.has(name));
  if (found.length) return;
  if (!L.onceInSession(evt, `tdd:${rel}`)) return;

  const subDir = path.posix.dirname(sub);
  const testPath = `tests/${svc}.Tests/${layer}/${subDir === '.' ? '' : `${subDir}/`}${cls}Tests.cs`;
  L.addContext(
    'PostToolUse',
    `[tdd] ${rel} foi criado sem teste. O projeto segue TDD: escreva ${testPath} (xUnit) cobrindo o comportamento ` +
    'desta classe e rode os testes antes de seguir. Se for só um DTO/record sem regra, diga isso ao usuário em vez de criar o teste.'
  );
});
