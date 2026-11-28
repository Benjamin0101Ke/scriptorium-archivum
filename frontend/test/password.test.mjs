import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluarPassword } from '../src/app/core/password.ts';

test('solo la longitud es obligatoria aunque haya símbolos y números', () => {
  assert.equal(evaluarPassword('A1!').longitud, false);
  assert.equal(evaluarPassword('abcdefgh').longitud, true);
  assert.equal(evaluarPassword('abcdefgh').puntos, 1);
});
test('respeta los límites de longitud', () => {
  for (const [length, valid] of [[0, false], [7, false], [8, true], [512, true], [513, false]])
    assert.equal(evaluarPassword('a'.repeat(length)).longitud, valid);
});
test('evalúa los tres criterios y reconoce letras Unicode', () => {
  assert.equal(evaluarPassword('abcdefgh!').puntos, 2);
  assert.equal(evaluarPassword('Ábcdefgh').puntos, 2);
  assert.equal(evaluarPassword('abcdefg1!').puntos, 3);
  assert.equal(evaluarPassword('abcdefgh ').simbolos, false);
  assert.equal(evaluarPassword('').etiqueta, 'Sin evaluar');
});
