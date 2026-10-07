const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// Unit test the existing shared helper with lightweight component doubles.
// No browser, database or credentials are involved.
const file = path.join(__dirname, '../src/02.Applications/02.WebApp/04.Web/wwwroot/js/custom-dxdropdownbox.min.js');
const source = fs.readFileSync(file, 'utf8');
const start = source.indexOf('function loadDxDropdownBoxColumn(');
const end = source.indexOf('// Versi SERVER-SIDE', start);
const boxes = new Map();
const inputs = new Map();
const grids = [];
function $(selector) {
  if (typeof selector === 'function') return selector();
  if (selector === '<div>') {
    const grid = { cleared: 0, deselectAll() { this.cleared++; }, clearSelection() {} };
    const wrapper = { dxDataGrid(options) {
      if (options === 'instance') return grid;
      grid.options = options;
      grids.push(grid);
      return wrapper;
    }};
    return wrapper;
  }
  return {
    val(value) { inputs.set(selector, value); },
    dxDropDownBox(options) {
      const listeners = {};
      const component = {
        getDataSource: () => options.dataSource,
        option(key, value) {
          if (arguments.length === 1) return options[key];
          const old = options[key]; options[key] = value;
          if (key === 'value' && old !== value) listeners.valueChanged?.({value});
        },
        field: () => ({val() {}}), close() {},
        on: (event, listener) => { listeners[event] = listener; },
      };
      boxes.set(selector, {options,component});
      options.contentTemplate({component});
    },
  };
}
const context = { $, baseUrl: '', renderDropdownColumns: c => c,
  DevExpress: { data: { CustomStore: function(options) { Object.assign(this,options); } } },
  window: {setTimeout: callback => callback()},
};
vm.createContext(context);
vm.runInContext(source.slice(start,end),context);
const columns = [{name:'Id'},{name:'Name'}];
context.loadDxDropdownBoxColumn('user','user_id','/UserFarm/Users','user-1',columns,false);
context.loadDxDropdownBoxColumn('farm','farm_id','/UserFarm/Farms','farm-1',columns,false);
assert.equal(boxes.get('#user').options.value,'user-1');
assert.equal(boxes.get('#farm').options.value,'farm-1');
grids[0].options.onSelectionChanged({selectedRowKeys:['user-2'],selectedRowsData:[{Id:'user-2',Name:'User Two'}]});
assert.equal(inputs.get('#user_id'),'user-2');
assert.equal(boxes.get('#farm').options.value,'farm-1');
boxes.get('#user').component.option('value',null);
assert.equal(grids[0].cleared,1);
assert.equal(grids[1].cleared,0);
grids[0].options.onSelectionChanged({selectedRowKeys:[],selectedRowsData:[]});
assert.equal(inputs.get('#user_id'),null);
assert.equal(boxes.get('#user').options.value,null);
assert.equal(boxes.get('#farm').options.value,'farm-1');
console.log('PASS: Edit values, selection, independent dropdowns and clear without restoring stale IDs');
context.loadDxDropdownBoxColumn('user_columns','user_value','/UserFarm/Users',null,
  [{name:'Id'},{name:'Username'},{name:'Name'},{name:'Email'}],false,2);
context.loadDxDropdownBoxColumn('farm_columns','farm_value','/UserFarm/Farms',null,
  [{name:'Id'},{name:'Code'},{name:'Name'}],false,2);
assert.equal(boxes.get('#user_columns').options.displayExpr({Username:'operator',Name:'Farm Operator',Email:'test@example.invalid'}),'Farm Operator');
assert.equal(boxes.get('#farm_columns').options.displayExpr({Code:'F-01',Name:'Farm One'}),'Farm One');
console.log('PASS: Multi-column User and Farm dropdowns display Name after selection');
