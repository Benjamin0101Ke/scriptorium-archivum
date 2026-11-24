import { Actualizaciones } from '../core/actualizaciones';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api, Rol } from '../core/api';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { CarreraSelector } from '../shared/carrera-selector';

interface CuentaRol {
  id: string;
  nombre: string;
  correo: string;
  rol: Rol;
  estado: string;
  id_docente: number | null;
  id_carrera: number | null;
  id_plan: number | null;
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
  imports: [FormsModule, RouterLink, DatePipe, CarreraSelector],
  templateUrl: './roles.html',
  styleUrl: './roles.css',
})
export class Roles {
 vivo=inject(Actualizaciones);
  api = inject(Api);
  usuarios = signal<CuentaRol[]>([]);
  roles = signal<Rol[]>([]);
  docentes = signal<Docente[]>([]);
  correos = signal<CorreoEvento[]>([]);
  correosConsultados = signal(false);
  docentesBorrador: Record<string, number | null> = {};
  error = signal('');
  message = signal('');
  estadoConfirmacion = signal<{id:string;texto:string}|null>(null);
  busy = signal(false);
  consulta = '';
  filtroRol = '';
  filtroEstado = '';
  actividad = signal<
    { id: string; fecha: string; nombre: string; evento: string; detalle: string }[]
  >([]);
  borradores: Record<string, Rol> = {};
  conectados = signal<{ total: number; usuarios: { id: string; nombre: string; rol: string }[] } | null>(null);
  presenciaError = signal('');
  editando = signal<CuentaRol | null>(null);
  nombreCorreccion = '';
  carreraCorreccion: number | null = null;
  planCorreccion: number | null = null;

  constructor() {
    this.vivo.watch(['cuentas'],()=>this.sincronizarCuentas(),inject(DestroyRef));
    this.vivo.watch(['presencia'],()=>this.actualizarConectados(),inject(DestroyRef));
    this.vivo.watch(['correos'],async()=>{if(this.correosConsultados())this.correos.set(await this.api.request('/admin/roles/correos'));},inject(DestroyRef));
    this.buscar();
    void this.actualizarConectados();
    const reloj = setInterval(() => { if (!document.hidden) void this.actualizarConectados(); }, 30000);
    inject(DestroyRef).onDestroy(() => clearInterval(reloj));
  }

  async actualizarConectados() {
    try { this.conectados.set(await this.api.request('/presencia')); this.presenciaError.set(''); }
    catch { this.presenciaError.set('No se pudo actualizar la conexión de las cuentas.'); }
  }

  puedeGestionar(cuenta: CuentaRol) {
    return cuenta.rol !== 'Superadministrador' || this.api.user()?.rol === 'Superadministrador';
  }

  editar(cuenta: CuentaRol) {
    this.nombreCorreccion = cuenta.nombre;
    this.carreraCorreccion = cuenta.id_carrera;
    this.planCorreccion = cuenta.id_plan;
    this.editando.set(cuenta);
  }

  async guardarPerfil() {
    const cuenta = this.editando();
    if (!cuenta) return;
    await this.ejecutar(async () => {
      const datos = { nombre: this.nombreCorreccion.trim(), id_carrera: this.carreraCorreccion, id_plan: this.planCorreccion };
      await this.api.request('/admin/roles/' + cuenta.id + '/perfil', 'PUT', datos);
      this.usuarios.update(items => items.map(item => item.id === cuenta.id ? { ...item, ...datos } : item));
      this.editando.set(null);
      this.message.set('Cuenta corregida. El cambio quedó registrado en la bitácora.');
    });
  }

  etiqueta(rol: Rol) {
    return rol === 'Biblioteca' ? 'Bibliotecario' : rol;
  }

  async sincronizarCuentas(){
    if(this.busy()||this.editando()||Object.keys(this.borradores).length||Object.keys(this.docentesBorrador).length)return;
    const query=new URLSearchParams({q:this.consulta.trim()});if(this.filtroRol)query.set('rol',this.filtroRol);if(this.filtroEstado)query.set('estado',this.filtroEstado);
    const data=await this.api.request<{usuarios:CuentaRol[];roles:Rol[];docentes:Docente[]}>('/admin/roles?'+query);
    if(this.busy()||this.editando()||Object.keys(this.borradores).length||Object.keys(this.docentesBorrador).length)return;
    this.usuarios.set(data.usuarios);this.roles.set(data.roles);this.docentes.set(data.docentes);
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
      this.estadoConfirmacion.set({id:cuenta.id,texto:habilitada
        ? 'Cuenta habilitada: ' + cuenta.nombre + '. Ya puede iniciar sesión.'
        : 'Cuenta deshabilitada: ' + cuenta.nombre + '. Su acceso quedó bloqueado.'});
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
    this.estadoConfirmacion.set(null);
    try {
      await operacion();
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
