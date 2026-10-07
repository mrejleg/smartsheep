const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../src/02.Applications/02.WebApp/04.Web/wwwroot/js/notification-menu.js'), 'utf8');

function element() {
  const classes = new Set();
  return { textContent: '', dataset: {}, classList: {
    add: x => classes.add(x), remove: x => classes.delete(x),
    contains: x => classes.has(x), toggle: (x, on) => on ? classes.add(x) : classes.delete(x),
  }, setAttribute() {}, removeAttribute() {} };
}
function setup(fetchResult) {
  const rows = [false, true].map(read => {
    const row = element();
    row.dataset = { isRead: String(read), readUrl: '/Notification/ReadNotification/test' };
    row.status = element(); row.error = element();
    row.status.textContent = read ? 'Read' : 'Unread';
    row.querySelector = q => q === '.notification-read-status' ? row.status : q === '.notification-read-error' ? row.error : null;
    row.closest = () => row;
    return row;
  });
  const badge = element(); badge.textContent = '1';
  const summary = element(); summary.textContent = '1 Unread';
  let click, requests = 0;
  const navigations = [];
  vm.runInNewContext(source, {
    URL,
    document: {
      addEventListener: (_, fn, capture) => { assert.equal(capture, true); click = fn; },
      getElementById: id => id === 'notification-unread-badge' ? badge : summary,
      querySelectorAll: () => rows.filter(x => x.dataset.isRead === 'false'),
    },
    window: { location: { href: 'https://localhost:44325/Dashboard', origin: 'https://localhost:44325', assign: url => navigations.push(url) } },
    fetch: async (...args) => { requests++; return fetchResult(...args); },
  });
  return { rows, badge, summary, navigations, requests: () => requests,
    click: row => click({ target: row, preventDefault() {}, stopPropagation() {} }) };
}

test('confirmed read keeps history and excludes read rows from the badge', async () => {
  const env = setup(async () => ({ ok: true, json: async () => ({ isRead: true }) }));
  await env.click(env.rows[0]);
  assert.equal(env.rows.length, 2);
  assert.equal(env.rows[0].status.textContent, 'Read');
  assert.equal(env.badge.textContent, '');
  assert.equal(env.badge.classList.contains('d-none'), true);
  assert.equal(env.summary.textContent, '0 Unread');
  await env.click(env.rows[0]);
  assert.equal(env.requests(), 1);
});
test('failure or unconfirmed result cannot mark read or decrement count', async () => {
  for (const response of [{ ok: false }, { ok: true, json: async () => ({ isRead: false }) }]) {
    const env = setup(async () => response);
    await env.click(env.rows[0]);
    assert.equal(env.rows[0].dataset.isRead, 'false');
    assert.equal(env.badge.textContent, '1');
    assert.match(env.rows[0].error.textContent, /Unable to mark/);
    assert.equal(env.navigations.length, 0);
  }
});
test('repeated pending clicks send only one request', async () => {
  let resolve;
  const pending = new Promise(r => { resolve = r; });
  const env = setup(() => pending);
  const first = env.click(env.rows[0]);
  await env.click(env.rows[0]);
  assert.equal(env.requests(), 1);
  resolve({ ok: true, json: async () => ({ isRead: true }) });
  await first;
  assert.equal(env.rows[0].dataset.reading, undefined);
});
test('only same-origin return links can navigate', async () => {
  const env = setup(async () => ({ ok: true, json: async () => ({ isRead: true, returnUrl: 'https://outside.example/test' }) }));
  await env.click(env.rows[0]);
  assert.equal(env.navigations.length, 0);
  env.rows[1].dataset.returnUrl = '/Dashboard';
  await env.click(env.rows[1]);
  assert.deepEqual(env.navigations, ['https://localhost:44325/Dashboard']);
});
