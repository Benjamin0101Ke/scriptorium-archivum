import type { User } from '../api';
import { validateMetadata } from '../metadata';
import { DemoData, error } from './datos';
import { persistPdf, validatePdf } from './ejemplar';

export interface Envio {
  id: string;
  titulo: string;
  tipo: string;
  estado: string;
  fecha: string;
}

export async function handleSubmissions(
  method: string,
  body: unknown,
  { works, logs }: DemoData,
  user: User,
  save: () => void,
) {
  if (user.rol !== 'Lector')
    error('Ingresá con una cuenta lectora para enviar tu trabajo.', {}, 403);
  const summary = (work: any): Envio => ({
    id: work.id,
    titulo: work.titulo,
    tipo: work.tipo,
    estado: work.estado,
    fecha: work.envio.fecha,
  });

  if (method === 'GET')
    return works.filter((work) => work.envio?.cuenta_id === user.id).map(summary);
  if (method !== 'POST' || !(body instanceof FormData)) error('Operación de envío no disponible.');

  const text = (name: string) => String(body.get(name) ?? '').trim();
  const metadata = {
    titulo: text('titulo'),
    autores: body
      .getAll('autores')
      .map((author) => String(author).trim())
      .filter(Boolean),
    anio: Number(text('anio')),
    carrera: text('carrera'),
    area: text('area'),
    tipo: text('tipo'),
    resumen: text('resumen'),
  };
  const fields = validateMetadata(metadata);
  if (fields.length) error('Revisá los campos marcados.', { fields });

  const file = await validatePdf(body.get('archivo'));
  const id = crypto.randomUUID();
  const exampleId = crypto.randomUUID();
  const fecha = new Date().toISOString();
  const work = {
    ...metadata,
    id,
    estado: 'En revision',
    envio: { cuenta_id: user.id, nombre: user.nombre, fecha },
    ejemplar_actual: exampleId,
    ejemplar: { id: exampleId, nombre_original: file.name },
    ficha_publica: false,
    lectura: false,
    descarga: false,
    enlaces: [],
  };
  works.unshift(work);
  logs.unshift({
    id: crypto.randomUUID(),
    nombre: user.nombre,
    fecha,
    evento: 'TRABAJO_ENVIADO_DEMO',
    trabajo_id: id,
  });
  logs.splice(200);
  await persistPdf(exampleId, file, save);
  return summary(work);
}
