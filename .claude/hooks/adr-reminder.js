'use strict';
// Hook 7 · PostToolUse (Write|Edit|MultiEdit)
// Lembra de registrar um ADR em docs/adr/ quando entra pacote ou referência nova, muda a topologia
// do Aspire (AppHost) ou um contrato de evento. Avisa uma vez por tipo de mudança em cada sessão.

const L = require('./lib');

const TRIGGERS = [
  { re: /(^|\/)Directory\.Packages\.props$/, test: (c) => /<PackageVersion\b/.test(c), label: 'versões de pacotes NuGet' },
  { re: /\.csproj$/, test: (c) => /<(PackageReference|ProjectReference)\b/.test(c), label: 'dependências de projeto/pacote' },
  { re: /^src\/AppHost\//, test: () => true, label: 'topologia do Aspire (AppHost)' },
  { re: /^src\/building-blocks\/[^/]*Contracts[^/]*\//, test: () => true, label: 'contratos de eventos' },
];

L.main((evt) => {
  const rel = L.relPath(evt, L.filePath(evt));
  if (!rel) return;
  const code = L.newContent(evt);
  const hit = TRIGGERS.find((t) => t.re.test(rel) && t.test(code));
  if (!hit || !L.onceInSession(evt, `adr:${hit.label}`)) return;

  L.addContext(
    'PostToolUse',
    `[adr] Mudança em ${hit.label} (${rel}). Se isso é uma decisão de arquitetura (pacote ou tecnologia nova, serviço novo, ` +
    'mudança de contrato), registre ao final da tarefa um ADR em docs/adr/NNNN-titulo-curto.md com Contexto, Decisão e ' +
    'Consequências. Se for só um detalhe sem decisão, ignore este aviso.'
  );
});
