const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.join(__dirname, '../src/02.Applications/02.WebApp/04.Web');
const read = file => fs.readFileSync(path.join(root, file), 'utf8');

test('filter icon uses the same size token as report card headers', () => {
  const css = read('wwwroot/css/report-dashboard-2026.css');
  const rule = css.match(/\.aims-toolbar-title svg \{([^}]+)\}/)[1];
  assert.match(rule, /width: var\(--modern-header-icon-size\)/);
  assert.match(rule, /height: var\(--modern-header-icon-size\)/);
  assert.match(rule, /background: var\(--modern-header-icon-bg\)/);
  assert.match(css, /\.aims-date-input \{[^}]*height: var\(--aims-btn-height\)/);
});

for (const report of ['SucklingActivityReport', 'SucklingStatisticReport', 'ReminderReport']) {
  test(`${report} shares dashboard-style filters and empty state`, () => {
    const view = read(`Views/Reports/${report}/Index.cshtml`);
    assert.match(view, /partial name="_ReportFilters"/);
    assert.match(view, /if \(Model.Count == 0\)[\s\S]*partial name="_ReportNoData"/);
    assert.match(view, /if \(Model.Count > 0 \|\| ViewBag.Page > 1\)/);
    assert.ok(view.indexOf('report-pager') > view.indexOf('</table>'));
    assert.match(view, /asp-route-from="@ViewBag.From"/);
    assert.match(view, /asp-route-to="@ViewBag.To"/);
  });
}

test('filter date names match existing report controller contracts', () => {
  const view = read('Views/Shared/_ReportFilters.cshtml');
  assert.match(view, /id="startDate"[^>]+name="from"/);
  assert.match(view, /id="endDate"[^>]+name="to"/);
  assert.equal((view.match(/data-aims-searchable="true"/g) || []).length, 2);
  assert.equal((view.match(/aria-label="(?:Search|Refresh)"/g) || []).length, 2);
});

test('farm change excludes other barns and resets a stale barn selection', () => {
  let change, barnChanges = 0;
  const barn = {
    options: [{ value: '' }, { value: 'a', dataset: { farmId: 'farm-a' } }, { value: 'b', dataset: { farmId: 'farm-b' } }],
    value: 'b',
    get selectedOptions() { return this.options.filter(o => o.value === this.value); },
    dispatchEvent() { barnChanges++; }
  };
  const farm = { value: '', addEventListener(_, fn) { change = fn; } };
  const toolbar = {
    dataset: {},
    querySelector(selector) { return selector === '.report-farm' ? farm : selector === '.report-barn' ? barn : null; }
  };
  const document = {
    readyState: 'complete',
    querySelectorAll(selector) { return selector === '.report-filter-toolbar' ? [toolbar] : []; },
    addEventListener() {}
  };
  vm.runInNewContext(read('wwwroot/js/report-filter-2026.js'), { document, Event: class {} });
  farm.value = 'FARM-A';
  change();
  assert.equal(barn.options[1].hidden, false);
  assert.equal(barn.options[2].hidden, true);
  assert.equal(barn.value, '');
  assert.equal(barnChanges, 1);
  farm.value = '';
  change();
  assert.equal(barn.options[2].hidden, false);
});
