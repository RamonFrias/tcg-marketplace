'use strict';
// Apoio ao hook 10 · UserPromptSubmit
// Guarda a hora em que o pedido começou, para o notify.js só avisar em tarefas longas.

const L = require('./lib');

L.main((evt) => {
  L.writeState(evt, 'prompt', { t: Date.now() });
});
