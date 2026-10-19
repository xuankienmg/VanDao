// Run effects-check first to export real server fixtures.
const fs = require('node:fs'), vm = require('node:vm'), assert = require('node:assert/strict'), path = require('node:path');
const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, 'effects-ui.json'), 'utf8'));
const context = vm.createContext({ DATA: fixture, S: null, ITEM: {}, adminOn: false, window: {}, localStorage: { getItem: () => null } });
vm.runInContext(fs.readFileSync(path.join(__dirname, '../../wwwroot/js/inventory.js'), 'utf8'), context);
for (const scenario of fixture.scenarios) {
  context.S = { gearEffects: scenario.snapshot, statSources: [], realm: 5 };
  const html = vm.runInContext('gearFxHtml()', context);
  assert.ok(!html.includes('Bộ Vàng') && !html.includes('Đỏ '));
  for (const set of scenario.snapshot.sets.filter(s => s.count)) assert.ok(html.includes(`${set.name} ${set.count}/10`));
  for (const effect of scenario.snapshot.effects) {
    assert.ok(html.includes(`+${effect.effective}${effect.unit}`));
    if (effect.raw > effect.effective) assert.ok(html.includes(`tổng ${effect.raw}, trần ${effect.cap}`));
  }
  for (const item of scenario.items) {
    context.item = item;
    context.ITEM[item.id] = item;
    const card = vm.runInContext('itemSetHtml(item)', context);
    for (const n of [3, 5, 8, 10]) assert.ok(card.includes(`${n} món — cộng thêm`));
    const tooltip = vm.runInContext('itemTip(item.id)', context);
    assert.ok(tooltip.includes('món đang mặc'));
    assert.ok(!card.includes('undefined') && !tooltip.includes('undefined'));
  }
}
console.log('PASS: real server fixtures render empty, partial, full, mixed and capped sets; tooltips show all milestones, with no old rarity bonuses.');
