import test from 'node:test';
import assert from 'node:assert/strict';
import { hoyElSalvador } from '../src/app/core/fechas.ts';

test('mantiene el día de El Salvador después del cambio de día en UTC', () => {
  assert.equal(hoyElSalvador(new Date('2026-11-18T02:30:00Z')), '2026-11-17');
});

test('cambia el día al llegar a medianoche en El Salvador', () => {
  assert.equal(hoyElSalvador(new Date('2026-11-18T05:59:59Z')), '2026-11-17');
  assert.equal(hoyElSalvador(new Date('2026-11-18T06:00:00Z')), '2026-11-18');
});

test('conserva el año local al cambiar de año en UTC', () => {
  assert.equal(hoyElSalvador(new Date('2027-01-01T01:00:00Z')), '2026-12-31');
});
