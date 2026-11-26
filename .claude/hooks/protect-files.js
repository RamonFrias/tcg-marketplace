'use strict';
// Hook 1 · PreToolUse (Write|Edit|MultiEdit|NotebookEdit)
// Bloqueia escrita em .git/, bin/, obj/, arquivos .env reais e qualquer segredo
// (senha, AccountKey, SharedAccessKey, chave privada, segredo em JSON) dentro do projeto.

const L = require('./lib');

// Chave pública e conhecida do Azurite (emulador local): não é segredo.
const AZURITE_KEY = 'Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==';

// Regras de connection string. Em arquivo de configuração valem no texto todo;
// em código (.cs etc.) só dentro de literais de string, para não confundir `user.Password = senha;`.
const CONNECTION_RULES = [
  [/\b(?:password|pwd)\s*=\s*([^;"'\s]+)/gi, 'uma senha em connection string'],
  [/\bAccountKey\s*=\s*([^;"'\s]+)/gi, 'uma AccountKey de Storage'],
  [/\bSharedAccessKey\s*=\s*([^;"'\s]+)/gi, 'uma SharedAccessKey de Service Bus'],
];
const JSON_RULE = [
  /"[\w.:-]*(?:secret|password|passwd|apikey|api_key|accesskey|signingkey|jwtkey|privatekey|token)"\s*:\s*"([^"]*)"/gi,
  'um segredo em JSON',
];
const CONFIG_FILE = /(\.(json|config|ya?ml|env|props|targets|xml|ini)$)|(^|\/)\.env(\.|$)/i;

function isPlaceholder(value) {
  const v = String(value).trim().replace(/^["']|["']$/g, '');
  if (!v || v === AZURITE_KEY) return true;
  return /^(<.*>|\$\{.*\}|\{.*\}|\{\{.*\}\}|__.*__|%.*%|\$\(.*\)|changeme|change-me|your[-_].*|x{3,}|\*+|SAS_KEY_VALUE)$/i.test(v);
}

function stringLiterals(code) {
  const out = [];
  const re = /@?\$?"((?:[^"\\]|\\.|"")*)"/g;
  let m;
  while ((m = re.exec(code))) out.push(m[1]);
  return out;
}

function firstHit(text, rules) {
  for (const [re, label] of rules) {
    re.lastIndex = 0;
    let m;
    while ((m = re.exec(text))) {
      if (!isPlaceholder(m[1])) return label;
    }
  }
  return null;
}

function findSecret(text, rel) {
  if (!text) return null;
  if (/-----BEGIN [A-Z ]*PRIVATE KEY-----/.test(text)) return 'uma chave privada';
  const haystacks = CONFIG_FILE.test(rel) ? [text] : stringLiterals(text);
  for (const h of haystacks) {
    const hit = firstHit(h, CONNECTION_RULES);
    if (hit) return hit;
  }
  return /\.json$/i.test(rel) ? firstHit(text, [JSON_RULE]) : null;
}

// Escopo de escrita dos agents do projeto (.claude/agents). Quem não está aqui não tem restrição extra.
const AGENT_SCOPES = {
  qa: {
    allowed: (rel) => rel.startsWith('tests/'),
    why: 'o agent qa só escreve em tests/. Reporte o bug no código de produção em vez de corrigi-lo.',
  },
  documentador: {
    allowed: (rel) => {
      const r = rel.toLowerCase();
      const base = r.split('/').pop();
      if (r.startsWith('.claude/') || base === 'claude.md') return false; // definições de agents, skills e instruções
      if (r.startsWith('docs/adr/')) return r === 'docs/adr/readme.md'; // ADR é histórico; só o índice
      return r.startsWith('docs/') ? r.endsWith('.md') : base === 'readme.md';
    },
    why: 'o agent documentador só escreve .md em docs/ e arquivos README.md, nunca em .claude/, CLAUDE.md ou ADRs (só o índice docs/adr/README.md). Divergência com ADR deve ser reportada.',
  },
};

L.main((evt) => {
  const rel = L.relPath(evt, L.filePath(evt));

  const scope = AGENT_SCOPES[evt.agent_type];
  if (scope && (!rel || !scope.allowed(rel))) {
    return L.deny(`[protect-files] ${rel || L.filePath(evt) || '(sem caminho)'}: ${scope.why}`);
  }

  if (!rel) return; // fora do projeto: não é responsabilidade deste hook

  const parts = rel.toLowerCase().split('/');
  const base = parts[parts.length - 1];

  if (parts.includes('.git')) {
    return L.deny(`[protect-files] ${rel} fica dentro de .git/. Não edite esses arquivos à mão; use comandos git.`);
  }
  if (parts.includes('bin') || parts.includes('obj')) {
    return L.deny(`[protect-files] ${rel} é saída de build (bin/obj), gerada pelo dotnet build. Altere o código-fonte, não o resultado da compilação.`);
  }
  if ((base === '.env' || base.startsWith('.env.')) && !/\.(example|sample|template)$/.test(base)) {
    return L.deny(`[protect-files] ${rel} guarda segredos e não vai para o Git. Crie ou edite um .env.example com valores de exemplo e peça ao usuário para preencher o .env real.`);
  }

  const secret = findSecret(L.newContent(evt), rel);
  if (secret) {
    return L.deny(
      `[protect-files] Encontrei ${secret} no que seria gravado em ${rel}. Segredo não entra no repositório: ` +
      'use dotnet user-secrets, variável de ambiente ou a connection string que o Aspire injeta. ' +
      'Valores de exemplo como <senha>, ${VAR}, SAS_KEY_VALUE (emulador do Service Bus) e UseDevelopmentStorage=true são aceitos.'
    );
  }
});
