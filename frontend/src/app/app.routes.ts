import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { Api, puedeEnviar, type Rol } from './core/api';
import { espaciosPorRol } from './core/roles';
import { Desarrolladores } from './pages/desarrolladores';
const personal: CanActivateFn = async () => {
  const api = inject(Api),
    router = inject(Router);
  await api.ready;
  return api.user() ? true : router.parseUrl('/ingresar');
};
const paraRol = (rol: Rol): CanActivateFn => async () => {
  const api = inject(Api),
    router = inject(Router);
  await api.ready;
  const user = api.user();
  if (!user) return router.parseUrl('/ingresar');
  return user.rol === rol ? true : router.parseUrl('/' + espaciosPorRol[user.rol].ruta);
};
const submissions: CanActivateFn = async () => {
  const api = inject(Api),
    router = inject(Router);
  await api.ready;
  const user = api.user();
  if (user?.rol === 'Biblioteca') return router.parseUrl('/biblioteca');
  if (!user) return router.createUrlTree(['/ingresar'], { queryParams: { destino: 'envios' } });
  return puedeEnviar(user.rol) ? true : router.parseUrl('/');
};
export const routes: Routes = [
  {
    path: 'admin/catalogos',
    canActivate: [paraRol('Administrador')],
    loadComponent: () => import('./pages/catalogos-admin').then((m) => m.CatalogosAdmin),
  },
  { path: '', loadComponent: () => import('./pages/catalogo').then((m) => m.Catalogo) },
  { path: 'trabajos/:id', loadComponent: () => import('./pages/ficha').then((m) => m.Ficha) },
  { path: 'ingresar', loadComponent: () => import('./pages/login').then((m) => m.Login) },
  {
    path: 'recuperar-contrasena',
    canActivate: [() => (inject(Api).demo ? inject(Router).parseUrl('/ingresar') : true)],
    loadComponent: () => import('./pages/recuperar').then((m) => m.Recuperar),
  },
  {
    path: espaciosPorRol.Administrador.ruta,
    canActivate: [paraRol('Administrador')],
    loadComponent: () => import('./pages/roles').then((m) => m.Roles),
  },
  {
    path: 'perfil',
    canActivate: [personal],
    loadComponent: () => import('./pages/perfil').then((m) => m.Perfil),
  },
  {
    path: 'favoritos',
    canActivate: [personal],
    loadComponent: () => import('./pages/favoritos').then((m) => m.Favoritos),
  },
  {
    path: 'mis-envios',
    canActivate: [submissions],
    loadComponent: () => import('./pages/envios').then((m) => m.Envios),
  },
  {
    path: espaciosPorRol.Biblioteca.ruta,
    canActivate: [paraRol('Biblioteca')],
    loadComponent: () => import('./pages/biblioteca').then((m) => m.Biblioteca),
  },
  {
    path: espaciosPorRol.Lector.ruta,
    canActivate: [paraRol('Lector')],
    loadComponent: () => import('./pages/espacios').then((m) => m.Lector),
  },
  {
    path: espaciosPorRol.Estudiante.ruta,
    canActivate: [paraRol('Estudiante')],
    loadComponent: () => import('./pages/espacios').then((m) => m.Estudiante),
  },
  {
    path: espaciosPorRol['Docente revisor'].ruta,
    canActivate: [paraRol('Docente revisor')],
    loadComponent: () => import('./pages/docente').then((m) => m.Docente),
  },
    {
  path: 'desarrolladores',
  component: Desarrolladores,
},
  { path: '**', redirectTo: '' },
];
