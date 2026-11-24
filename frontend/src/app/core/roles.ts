import type { Rol } from './api';

export const espaciosPorRol = {
  Lector: { ruta: 'lector', enlace: 'Mi espacio' },
  Estudiante: { ruta: 'estudiante', enlace: 'Mi espacio' },
  'Docente revisor': { ruta: 'docente', enlace: 'Panel docente' },
  Biblioteca: { ruta: 'biblioteca', enlace: 'Biblioteca' },
  Administrador: { ruta: 'admin/roles', enlace: 'Roles' },
  Superadministrador: { ruta: 'superadmin', enlace: 'Superadministración' },
} satisfies Record<Rol, { ruta: string; enlace: string }>;
