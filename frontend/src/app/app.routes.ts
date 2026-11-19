import { inject } from '@angular/core';
import { CanActivateFn, Router, Routes } from '@angular/router';
import { Api } from './core/api';
const personal: CanActivateFn = async () => {
  const api = inject(Api),
    router = inject(Router);
  await api.ready;
  return api.user() ? true : router.parseUrl('/ingresar');
};
const library: CanActivateFn = async () => {
  const api = inject(Api),
    router = inject(Router);
  await api.ready;
  return api.user()?.rol === 'Biblioteca' ? true : router.parseUrl('/ingresar');
};
const submissions: CanActivateFn = async () => {
  const api = inject(Api),
    router = inject(Router);
  if (!api.demo) return router.parseUrl('/');
  await api.ready;
  const user = api.user();
  if (user?.rol === 'Biblioteca') return router.parseUrl('/biblioteca');
  return user ? true : router.createUrlTree(['/ingresar'], { queryParams: { destino: 'envios' } });
};
export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/catalogo').then((m) => m.Catalogo) },
  { path: 'trabajos/:id', loadComponent: () => import('./pages/ficha').then((m) => m.Ficha) },
  { path: 'ingresar', loadComponent: () => import('./pages/login').then((m) => m.Login) },
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
    path: 'biblioteca',
    canActivate: [library],
    loadComponent: () => import('./pages/biblioteca').then((m) => m.Biblioteca),
  },
  { path: '**', redirectTo: '' },
];
