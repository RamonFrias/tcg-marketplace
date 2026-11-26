'use strict';
// Hook 10 · Notification e Stop (assíncrono)
// Mostra uma notificação do Windows quando o Claude está esperando você (Notification)
// ou quando termina uma tarefa que levou pelo menos MIN_SECONDS (Stop).
// No Stop, espera o build-test-on-stop terminar: se ele devolveu o turno ao Claude, ainda não acabou.
// TCG_HOOKS_NOTIFY_DRY=1 imprime a notificação em vez de exibi-la; TCG_HOOKS_NOTIFY_DELAY_MS ajusta a espera inicial.

const cp = require('child_process');
const L = require('./lib');

const MIN_SECONDS = 60;
const TITLE = 'Claude · tcg-marketplace';
const WAIT_BUILD_MS = 620000;

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

function toast(text) {
  const message = String(text).slice(0, 240);
  if (process.env.TCG_HOOKS_NOTIFY_DRY) {
    process.stdout.write(JSON.stringify({ toast: message }));
    return;
  }
  if (process.platform !== 'win32') return;
  // O texto vai por variável de ambiente, nunca dentro do script: nada do conteúdo é interpretado pelo PowerShell.
  const script = [
    'Add-Type -AssemblyName System.Windows.Forms',
    'Add-Type -AssemblyName System.Drawing',
    '$n = New-Object System.Windows.Forms.NotifyIcon',
    '$n.Icon = [System.Drawing.SystemIcons]::Information',
    '$n.BalloonTipTitle = $env:TCG_TOAST_TITLE',
    '$n.BalloonTipText = $env:TCG_TOAST_TEXT',
    '$n.Visible = $true',
    '$n.ShowBalloonTip(8000)',
    'Start-Sleep -Seconds 9',
    '$n.Dispose()',
  ].join('; ');
  const child = cp.spawn(
    'powershell.exe',
    ['-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-EncodedCommand', Buffer.from(script, 'utf16le').toString('base64')],
    {
      detached: true,
      stdio: 'ignore',
      windowsHide: true,
      env: { ...process.env, TCG_TOAST_TITLE: TITLE, TCG_TOAST_TEXT: message },
    }
  );
  child.unref();
}

L.main(async (evt) => {
  if (evt.hook_event_name === 'Notification') {
    toast(evt.message || 'O Claude está esperando você.');
    return;
  }
  if (evt.hook_event_name !== 'Stop') return;

  const started = L.readState(evt, 'prompt', null);
  if (!started || !started.t || started.notified) return;

  // Dá tempo do build-test-on-stop marcar "running" e espera ele terminar.
  await sleep(Number(process.env.TCG_HOOKS_NOTIFY_DELAY_MS ?? 1500));
  const limit = Date.now() + WAIT_BUILD_MS;
  let result = L.readState(evt, 'stop-result', null);
  while (result && result.t >= started.t && result.status === 'running' && Date.now() < limit) {
    await sleep(2000);
    result = L.readState(evt, 'stop-result', null);
  }
  if (result && result.t >= started.t && result.status === 'blocked') return; // o Claude voltou a trabalhar

  const seconds = (Date.now() - started.t) / 1000;
  if (seconds < MIN_SECONDS) return;
  L.writeState(evt, 'prompt', { ...started, notified: true });
  toast(`Tarefa concluída em ${Math.max(1, Math.round(seconds / 60))} min. Volte para revisar.`);
});
