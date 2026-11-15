import type { User } from '../api';

export interface LocalAccount {
  user: User;
  salt: string;
  verifier: string;
}

export const seed = [
  {
    id: 'demo-01',
    titulo: 'Gestión documental para la memoria académica',
    autores: ['Autora de ejemplo'],
    anio: 2026,
    carrera: 'Ingeniería de Sistemas Informáticos',
    area: 'Gestión documental',
    tipo: 'Trabajo de grado',
    resumen:
      'Ficha ficticia para revisar el diseño del catálogo. Describe una propuesta de organización y consulta del patrimonio documental universitario.',
  },
  {
    id: 'demo-02',
    titulo: 'Accesibilidad en servicios digitales universitarios',
    autores: ['Autor de ejemplo'],
    anio: 2025,
    carrera: 'Ingeniería de Sistemas Informáticos',
    area: 'Accesibilidad',
    tipo: 'Proyecto',
    resumen:
      'Datos de demostración para comprobar filtros, navegación y lectura de metadatos en diferentes tamaños de pantalla.',
  },
  {
    id: 'demo-03',
    titulo: 'Análisis de información para bibliotecas académicas',
    autores: ['Equipo de ejemplo'],
    anio: 2024,
    carrera: 'Ingeniería de Sistemas Informáticos',
    area: 'Análisis de datos',
    tipo: 'Trabajo de grado',
    resumen:
      'Registro ficticio sin PDF ni autorización institucional. Se utiliza exclusivamente para revisar la presentación de la colección.',
  },
];
export interface DemoData {
  works: any[];
  favorites: Record<string, string[]>;
  logs: {
    id: string;
    nombre: string;
    fecha: string;
    evento: string;
    trabajo_id: string;
    motivo?: string;
  }[];
  accounts: LocalAccount[];
}
export const initialData = (): DemoData => ({
  works: seed.map((t) => ({
    ...t,
    estado: 'Publicado',
    lectura: false,
    descarga: false,
    enlaces: [],
    controles: [],
  })),
  favorites: {},
  logs: [],
  accounts: [],
});
export function error(message: string, details = {}, status?: number): never {
  throw Object.assign(new Error(message), { details, status });
}

export const normalize = (value: unknown) =>
  String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();
