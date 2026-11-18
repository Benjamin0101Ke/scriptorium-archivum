import type { User } from '../api';
import { readPdf } from './almacenamiento';
import { requireUser } from './cuentas';
import { DemoData, error, normalize as norm } from './datos';
import { isPublic } from './publicacion';

function publicWork(t: any) {
  const { id, titulo, autores, anio, carrera, area, tipo, resumen, lectura, descarga } = t;
  return {
    id,
    titulo,
    autores,
    anio,
    carrera,
    area,
    tipo,
    resumen,
    lectura,
    descarga,
    enlaces: t.enlaces
      .filter((link: any) => link.habilitado)
      .map(({ id, url, tipo, descripcion }: any) => ({ id, url, tipo, descripcion })),
  };
}

export async function handleCatalog(
  url: URL,
  method: string,
  { works, favorites }: DemoData,
  user: User | undefined,
  save: () => void,
) {
  const p = url.pathname;
  if (p.startsWith('/favoritos')) {
    const { id: account } = requireUser(user);
    const list = new Set(favorites[account] || []);
    if (method === 'GET') return works.filter((t) => list.has(t.id) && isPublic(t)).map(publicWork);
    const id = p.split('/')[2];
    if (method === 'PUT') {
      if (!works.some((t) => t.id === id && isPublic(t))) error('Ficha no disponible.');
      list.add(id);
    }
    if (method === 'DELETE') list.delete(id);
    favorites[account] = [...list];
    save();
    return { ok: true };
  }
  if (p === '/trabajos') {
    const q = url.searchParams,
      terms = [...new Set(norm(q.get('q')).split(/\s+/).filter(Boolean))];
    const score = (t: any) =>
      ['titulo', 'area', 'resumen'].map(
        (f) => terms.filter((term) => norm(t[f]).includes(term)).length,
      );
    const title = (a: any, b: any) =>
      norm(a.titulo).localeCompare(norm(b.titulo), 'es') || a.id.localeCompare(b.id);
    return works
      .filter(
        (t) =>
          isPublic(t) &&
          terms.every((term) => norm(t.titulo + ' ' + t.area + ' ' + t.resumen).includes(term)) &&
          ['autor', 'anio', 'carrera', 'area', 'tipo'].every(
            (f) =>
              !q.get(f) ||
              (f === 'autor'
                ? t.autores.some((a: string) => norm(a) === norm(q.get(f)))
                : norm(t[f]) === norm(q.get(f))),
          ),
      )
      .sort((a, b) => {
        if (q.get('orden') === 'anio') return b.anio - a.anio || title(a, b);
        if (q.get('orden') === 'titulo') return title(a, b);
        const x = score(a),
          y = score(b);
        return y[0] - x[0] || y[1] - x[1] || y[2] - x[2] || title(a, b);
      })
      .map(publicWork);
  }

  const [, resource, id, action] = p.split('/');
  const work = works.find((item) => item.id === id);
  if (resource !== 'trabajos' || !work || !isPublic(work)) error('Ficha no disponible.');
  if (action === 'pdf') {
    const operation = url.searchParams.get('operacion');
    if (!((operation === 'lectura' && work.lectura) || (operation === 'descarga' && work.descarga)))
      error('La ficha no permite esta operación con el PDF.');
    return (
      (await readPdf(work.ejemplar_actual)) ??
      error('El PDF ya no está disponible en este navegador.')
    );
  }
  return publicWork(work);
}
