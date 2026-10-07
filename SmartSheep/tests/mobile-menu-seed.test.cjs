const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.join(__dirname, '../src/02.Applications/01.WebApi/02.Infrastructure/Data/DbContexts');

for (const file of ['ApplicationDbContextSeed.cs', 'SeedingData/ApiManagementDbContextSeed.cs']) {
  test(`${file} preserves existing mobile visibility during startup`, () => {
    const source = fs.readFileSync(path.join(root, file), 'utf8');
    // Initializers for new menus may set a default; existing menus must retain
    // the administrator's value, including false, across every startup.
    assert.doesNotMatch(source, /\b\w+\.IsMobile\s*=(?!=)/);
    assert.doesNotMatch(source, /SeedMobileMenuVisibilityAsync/);
  });
}

test('startup never runs menu renaming, merging or icon repair against existing data', () => {
  const seed = fs.readFileSync(path.join(root, 'ApplicationDbContextSeed.cs'), 'utf8');
  const entry = seed.slice(0, seed.indexOf('public static async Task FixMenuIconsAsync'));
  assert.match(entry, /var seedMenus = !await applicationDbContext\.AppMenu\.AnyAsync\(\)/);
  assert.match(entry, /if \(seedMenus\)\s*\{[^}]*AddLocalUserMenusAsync[^}]*AddSmartSheepDataAndMenusAsync[^}]*AddMenuIconsAsync/);
  assert.doesNotMatch(entry, /await (RenameMenusAsync|DeduplicateMenusAsync|ConfigDbContextSeed\.ReconcileAppMenuRolesAsync)/);
  const program = fs.readFileSync(path.join(root, '../../../04.Api/Program.cs'), 'utf8');
  assert.doesNotMatch(program, /FixMenuIconsAsync/);
});
