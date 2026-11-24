import type { User } from '../api';
import { validateMetadata } from '../metadata';
import { deletePdf, readPdf } from './almacenamiento';
import { DemoData, error, normalize as norm } from './datos';
import { persistPdf, validatePdf } from './ejemplar';
import { checks, isPublic } from './publicacion';

const copy = structuredClone;

export async function handleLibrary(
  p: string,
  method: string,
  body: any,
  { works, logs }: DemoData,
  user: User,
  save: () => void,
  expectedExemplar: string | null = null,
) {
  if (user.rol !== 'Biblioteca')
    error('Selecciona la cuenta de Biblioteca de demostración.', {}, 403);
  const log = (t: any, evento: string, motivo?: string) => {
    logs.unshift({
      id: crypto.randomUUID(),
      nombre: user.nombre,
      fecha: new Date().toISOString(),
      evento,
      trabajo_id: t.id,
      motivo,
    });
    logs.splice(200);
  };
  if (p === '/admin/guia')
    return {
      disponible: false,
      mensaje:
        'Pendiente: incorporar la guía institucional aprobada cuando la API esté disponible.',
    };
  if (p === '/admin/bitacora') return copy(logs);
  if (p === '/admin/reporte') {
    const years = [...new Set(works.map((t) => t.deposito_fecha?.slice(0, 4) || null))];
    return years.map((anio) => {
      const list = works.filter((t) => (t.deposito_fecha?.slice(0, 4) || null) === anio);
      return {
        anio,
        registradas: list.length,
        publicadas: list.filter((t) => t.estado === 'Publicado').length,
      };
    });
  }
  if (p.startsWith('/admin/enlaces/')) {
    const id = p.split('/')[3];
    const link = works.flatMap((t) => t.enlaces || []).find((l) => l.id === id);
    if (!link) error('Enlace no encontrado.');
    link.habilitado = body.habilitado;
    save();
    return { ok: true };
  }
  if (p === '/admin/trabajos' && method === 'GET') return copy(works);
  if (p === '/admin/trabajos' && method === 'POST') {
    const fields = validateMetadata(body);
    if (fields.length) error('Revisa estos campos: ' + fields.join(', '), { fields });
    const authors = (v: any) => v.autores.map(norm).sort().join('|');
    if (
      !body.confirmarDuplicado &&
      works.some(
        (t) =>
          norm(t.titulo) === norm(body.titulo) &&
          t.anio === body.anio &&
          authors(t) === authors(body),
      )
    )
      error('Posible duplicado.', { duplicate: true });
    const { confirmarDuplicado, ...metadata } = body;
    const t = {
      ...copy(metadata),
      id: crypto.randomUUID(),
      estado: 'Borrador',
      enlaces: [],
      lectura: false,
      descarga: false,
    };
    works.unshift(t);
    log(t, 'FICHA_CREADA_DEMO');
    save();
    return copy(t);
  }

  const [, , , id, action] = p.split('/');
  const t = works.find((item) => item.id === id);
  if (!t) error('Ficha no disponible.');
  if (!action && method === 'GET') return { ...copy(t), controles: checks(t) };
  if (!action && method === 'PUT') {
    const fields = validateMetadata(body);
    if (fields.length) error('Revisa estos campos: ' + fields.join(', '), { fields });
    const { confirmarDuplicado, ...metadata } = body;
    Object.assign(t, copy(metadata));
    log(t, 'METADATOS_CORREGIDOS_DEMO');
    save();
    return copy(t);
  }
  if (action === 'pdf' || action === 'referencia') {
    if (action === 'pdf' && method === 'GET') {
      if (expectedExemplar !== null && expectedExemplar !== t.ejemplar_actual)
        error('El ejemplar cambió. Recarga la ficha.', {}, 404);
      if (!t.ejemplar?.nombre_original) error('Esta ficha no tiene un PDF asociado.');
      return (await readPdf(t.ejemplar_actual)) ?? error('El PDF no está disponible en este navegador.');
    }
    if (method !== 'POST') error('Operación de ejemplar no disponible.');
    const f = action === 'pdf' ? await validatePdf(body.get('archivo')) : null;
    const referencia = action === 'referencia' ? body.referencia?.trim() : '';
    if (action === 'referencia' && !referencia)
      error('Ingresa una referencia institucional verificable.');
    const previous = t.ejemplar_actual;
    const id = crypto.randomUUID();
    const ejemplar = f
      ? { id, nombre_original: f.name }
      : { id, referencia_institucional: referencia };
    Object.assign(t, {
      ejemplar_actual: id,
      ejemplar,
      autorizacion: null,
      decision: null,
      estado: 'En revision',
      deposito_verificado: false,
      ficha_publica: false,
      lectura: false,
      descarga: false,
    });
    log(t, 'EJEMPLAR_SELECCIONADO_DEMO');
    await persistPdf(id, f, save);
    if (previous) await deletePdf(previous).catch(() => undefined);
    return { ejemplar_id: id };
  }
  if (action === 'controles') {
    if (!t.ejemplar || body.ejemplar_id !== t.ejemplar_actual)
      error('El ejemplar cambió. Recarga la ficha.', {}, 409);
    const { verificar, conformidad_referencia, ejemplar_id, registrar_decision, decision, autorizacion, ...b } = copy(body);
    if (registrar_decision && (!decision?.motivo?.trim() || !['Favorable', 'Desfavorable'].includes(decision.resultado)))
      error('Indica el resultado y el motivo de la decisión de Biblioteca.');
    if (verificar) {
      if (!conformidad_referencia?.trim()) error('Falta la referencia de conformidad.');
      Object.assign(t.ejemplar, {
        conformidad_referencia,
        verificador: user.nombre,
        fecha_verificacion: new Date().toISOString(),
      });
    }
    Object.assign(t, b);
    if (autorizacion) t.autorizacion = autorizacion;
    if (registrar_decision) {
      t.decision = { ...decision, fecha: new Date().toISOString(), responsable: user.nombre };
    }
    if (t.estado === 'Publicado' && !isPublic(t)) t.estado = 'En revision';
    log(t, 'CONTROLES_REGISTRADOS_DEMO');
    save();
    return copy(t);
  }
  if (action === 'publicar') {
    const controles = checks(t);
    if (controles.some((c) => !c.ok))
      error('Faltan condiciones de publicación en la simulación.', { controles });
    t.estado = 'Publicado';
    log(t, 'PUBLICACION_SIMULADA');
    save();
    return { ok: true };
  }
  if (action === 'retirar') {
    const motivo = body.motivo?.trim();
    if (!motivo) error('El retiro requiere motivo.');
    t.estado = 'Retirado';
    log(t, 'RETIRO_SIMULADO', motivo);
    save();
    return { ok: true };
  }
  if (action === 'enlaces') {
    if (method === 'GET') return copy(t.enlaces);
      if (method === 'DELETE') {
    const linkId = p.split('/').pop();
const index = (t.enlaces || []).findIndex((l: any) => l.id === linkId);
    if (index === -1) error('Enlace no encontrado.');

    t.enlaces.splice(index, 1);
    save();
    return { ok: true };
  }
    let url: URL;
    try {
      url = new URL(body.url);
    } catch {
      error('Ingresa una URL válida.');
      return;
    }
    if (url.protocol !== 'https:' || !body.tipo?.trim() || !body.descripcion?.trim())
      error('Usa HTTPS e indica tipo y descripción.');
    t.enlaces.push({ ...copy(body), id: crypto.randomUUID(), habilitado: false });
    save();
    return { ok: true };
  }
  error('Esta operación está pendiente de integrar con la API de Roberto.');
}
