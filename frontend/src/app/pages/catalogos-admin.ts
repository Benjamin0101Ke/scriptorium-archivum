import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../core/api';

interface RegistroCatalogo {
  id: number;
  nombre: string;
  activa: boolean;
  id_departamento?: number;
  id_carrera?: number;
  url?: string | null;
}

@Component({
  selector: 'app-catalogos-admin',
  imports: [FormsModule, RouterLink],
  templateUrl: './catalogos-admin.html',
})
export class CatalogosAdmin {
  private api = inject(Api);
  tipo = 'carreras';
  registros = signal<RegistroCatalogo[]>([]);
  departamentos = signal<RegistroCatalogo[]>([]);
  carreras = signal<RegistroCatalogo[]>([]);
  busqueda = '';
  nombre = '';
  idDepartamento: number | null = null;
  idCarrera: number | null = null;
  url = '';
  activa = true;
  editando: number | null = null;
  busy = signal(false);
  error = signal('');
  message = signal('');
  constructor() {
    this.cargar();
  }
  filtrados() {
    const texto = this.busqueda
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase();
    return this.registros().filter((r) =>
      r.nombre
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '')
        .toLowerCase()
        .includes(texto),
    );
  }
  nuevo() {
    this.editando = null;
    this.nombre = '';
    this.idDepartamento = null;
    this.idCarrera = null;
    this.url = '';
    this.activa = true;
  }
  editar(registro: RegistroCatalogo) {
    this.editando = registro.id;
    this.nombre = registro.nombre;
    this.idDepartamento = registro.id_departamento ?? null;
    this.activa = registro.activa;
    this.idCarrera = registro.id_carrera ?? null;
    this.url = registro.url ?? '';
  }
  async cargar() {
    this.busy.set(true);
    this.error.set('');
    this.nuevo();
    this.busqueda = '';
    try {
      const [registros, departamentos, carreras] = await Promise.all([
        this.api.request<RegistroCatalogo[]>('/admin/catalogos/' + this.tipo),
        this.api.request<RegistroCatalogo[]>('/admin/catalogos/departamentos'),
        this.api.request<RegistroCatalogo[]>('/admin/catalogos/carreras'),
      ]);
      this.registros.set(registros);
      this.departamentos.set(departamentos);
      this.carreras.set(carreras);
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
  async guardar() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await this.api.request(
        '/admin/catalogos/' + this.tipo + (this.editando ? '/' + this.editando : ''),
        this.editando ? 'PUT' : 'POST',
        {
          nombre: this.nombre,
          id_departamento: this.idDepartamento,
          activa: this.activa,
          id_carrera: this.idCarrera,
          url: this.url,
        },
      );
      this.registros.set(await this.api.request('/admin/catalogos/' + this.tipo));
      this.departamentos.set(await this.api.request('/admin/catalogos/departamentos'));
      this.nuevo();
      this.message.set('Catálogo guardado.');
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
