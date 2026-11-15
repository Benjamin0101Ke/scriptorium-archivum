import { validateMetadata } from '../metadata';
import { seed } from './datos';

export function checks(t: any) {
  const a = t.autorizacion,
    e = t.ejemplar,
    d = t.decision;
  const valid =
    a?.estado === 'Vigente' &&
    a?.referencia?.trim() &&
    a?.otorgantes?.trim() &&
    a?.fecha &&
    (!a.vence || a.vence >= new Date().toISOString().slice(0, 10));
  return [
    [validateMetadata(t).length === 0, '01 · Siete metadatos completos'],
    [t.deposito_verificado && t.deposito_referencia, '02 · Depósito UES verificado'],
    [e?.conformidad_referencia && e?.verificador, '03 · Ejemplar final verificado'],
    [valid, '04 · Autorización vigente para este ejemplar'],
    [d?.resultado === 'Favorable' && d?.motivo, '05 · Decisión favorable de Biblioteca'],
    [valid && a.ficha && t.ficha_publica, '06 · Difusión de ficha autorizada'],
    [
      valid && (!t.lectura || a.lectura) && (!t.descarga || a.descarga),
      '07 · Lectura y descarga dentro de lo autorizado',
    ],
    [!(t.lectura || t.descarga) || e?.nombre_original, '08 · PDF del ejemplar actual'],
    [['Borrador', 'En revision'].includes(t.estado), '09 · Estado de origen y rol Biblioteca'],
  ].map(([ok, label]) => ({ ok: !!ok, label }));
}
export function isPublic(t: any): boolean {
  return (
    t.estado === 'Publicado' &&
    ((seed.some((item) => item.id === t.id) && !t.ejemplar) ||
      checks(t)
        .slice(0, 8)
        .every((check) => check.ok))
  );
}
