import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api, Rol } from '../core/api';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';

interface CuentaRol {
  id: string;
  nombre: string;
  correo: string;
  rol: Rol;
  estado: string;
  id_docente: number | null;
}

interface Docente {
  id: number;
  nombre: string;
  correo: string;
}
interface CorreoEvento {
  id: string;
  destino: string;
  asunto: string;
  estado: string;
  intentos: number;
  fecha: string;
  enviado_en: string | null;
  error_tipo: string | null;
  evento: string;
}

@Component({
  selector: 'app-roles',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './roles.html',
})
export class Roles {
  api = inject(Api);
  usuarios = signal<CuentaRol[]>([]);
  roles = signal<Rol[]>([]);
  docentes = signal<Docente[]>([]);
  correos = signal<CorreoEvento[]>([]);
  correosConsultados = signal(false);
  docentesBorrador: Record<string, number | null> = {};
  error = signal('');
  message = signal('');
  busy = signal(false);
  consulta = '';
  filtroRol = '';
  filtroEstado = '';
  actividad = signal<
    { id: string; fecha: string; nombre: string; evento: string; detalle: string }[]
  >([]);
  borradores: Record<string, Rol> = {};

  constructor() {
    this.buscar();
  }

  etiqueta(rol: Rol) {
    return rol === 'Biblioteca' ? 'Bibliotecario' : rol;
  }

  async buscar() {
    await this.ejecutar(async () => {
      const query = new URLSearchParams({ q: this.consulta.trim() });
      if (this.filtroRol) query.set('rol', this.filtroRol);
      if (this.filtroEstado) query.set('estado', this.filtroEstado);
      const data = await this.api.request<{
        usuarios: CuentaRol[];
        roles: Rol[];
        docentes: Docente[];
      }>('/admin/roles?' + query);
      this.borradores = {};
      this.docentesBorrador = {};
      this.docentes.set(data.docentes);
      this.usuarios.set(data.usuarios);
      this.roles.set(data.roles);
    });
  }

  async guardar(cuenta: CuentaRol) {
    const proximo = this.borradores[cuenta.id];
    if (!proximo || proximo === cuenta.rol) return;
    await this.ejecutar(async () => {
      await this.api.request('/admin/roles/' + encodeURIComponent(cuenta.id), 'PUT', {
        rol: proximo,
      });
      this.usuarios.update((items) =>
        items.map((item) => (item.id === cuenta.id ? { ...item, rol: proximo } : item)),
      );
      delete this.borradores[cuenta.id];
      this.message.set('Rol actualizado para ' + cuenta.nombre + '.');
    });
  }

  async cambiarEstado(cuenta: CuentaRol) {
    await this.ejecutar(async () => {
      const habilitada = cuenta.estado !== 'Habilitada';
      await this.api.request('/admin/roles/' + cuenta.id + '/estado', 'PUT', { habilitada });
      this.usuarios.update((items) =>
        items.map((item) =>
          item.id === cuenta.id
            ? { ...item, estado: habilitada ? 'Habilitada' : 'Deshabilitada' }
            : item,
        ),
      );
      this.message.set('Estado actualizado para ' + cuenta.nombre + '.');
    });
  }

  async recuperar(cuenta: CuentaRol) {
    await this.ejecutar(async () => {
      await this.api.request('/admin/roles/' + cuenta.id + '/recuperacion', 'POST');
      this.message.set('Solicitud de recuperación enviada al correo de ' + cuenta.nombre + '.');
    });
  }

  async verActividad() {
    await this.ejecutar(async () => {
      this.actividad.set(await this.api.request('/admin/roles/actividad'));
    });
  }

  docenteElegido(cuenta: CuentaRol) {
    return Object.hasOwn(this.docentesBorrador, cuenta.id)
      ? this.docentesBorrador[cuenta.id]
      : (cuenta.id_docente ?? null);
  }

  docenteDisponible(id: number | null) {
    return !id || this.docentes().some((docente) => docente.id === id);
  }

  async guardarDocente(cuenta: CuentaRol) {
    const id_docente = this.docenteElegido(cuenta);
    await this.ejecutar(async () => {
      await this.api.request('/admin/roles/' + cuenta.id + '/docente', 'PUT', { id_docente });
      this.usuarios.update((items) =>
        items.map((item) => (item.id === cuenta.id ? { ...item, id_docente } : item)),
      );
      delete this.docentesBorrador[cuenta.id];
      this.message.set('Docente actualizado. Los próximos avisos llegarán al docente asignado.');
    });
  }

  async verCorreos() {
    await this.ejecutar(async () => {
      this.correos.set(await this.api.request<CorreoEvento[]>('/admin/roles/correos'));
      this.correosConsultados.set(true);
    });
  }

  private async ejecutar(operacion: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operacion();
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
