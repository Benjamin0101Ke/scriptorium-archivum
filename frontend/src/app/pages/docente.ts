import { Component, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';

export interface RevisionDocente {
  resultado: 'Conforme' | 'Requiere cambios';
  observaciones: string;
  fecha: string;
  docente: string;
  version: number;
}

export interface TrabajoDocente {
  id: string;
  titulo: string;
  autores: string[];
  anio: number;
  carrera: string;
  tipo: string;
  estado: string;
  estudiante: string;
  id_estudiante: string;
  version: number;
  id_entrega: string;
  estado_revision: 'pendiente' | 'conforme' | 'cambios';
  revision_docente?: RevisionDocente | null;
  historial?: RevisionDocente[];
}

export interface ResumenDocente {
  docentes: number;
  pendientes: number;
  conformes: number;
  cambios: number;
}

@Component({
  selector: 'app-docente',
  imports: [DatePipe, FormsModule],
  templateUrl: './docente.html',
  styleUrl: './docente.css',
})
export class Docente {
  api = inject(Api);
  items = signal<TrabajoDocente[]>([]);
  resumen = signal<ResumenDocente | null>(null);
  cargando = signal(true);
  error = signal('');
  mensaje = signal('');
  busqueda = signal('');
  estado = signal<'todas' | 'pendiente' | 'conforme' | 'cambios'>('todas');
  revisandoId = signal<string | null>(null);
  resultado = signal<'Conforme' | 'Requiere cambios'>('Conforme');
  observaciones = signal('');
  busy = signal(false);

  filtrados = computed(() => this.items());

  async ngOnInit() {
    await this.cargar();
  }

  async cargar() {
    this.cargando.set(true);
    this.error.set('');
    try {
      const params = new URLSearchParams();
      if (this.busqueda().trim()) params.set('q', this.busqueda().trim());
      if (this.estado() !== 'todas') params.set('estado', this.estado());
      const query = params.toString();
      const [resumen, items] = await Promise.all([
        this.api.request<ResumenDocente>('/docente/resumen'),
        this.api.request<TrabajoDocente[]>(`/docente/trabajos${query ? '?' + query : ''}`),
      ]);
      this.resumen.set(resumen);
      this.items.set(items);
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.cargando.set(false);
    }
  }

  async buscar() {
    await this.cargar();
  }

  seleccionarFiltro(f: string) {
    this.estado.set(f as 'todas' | 'pendiente' | 'conforme' | 'cambios');
    this.cargar();
  }

  abrirRevision(id: string) {
    const trabajo = this.items().find((t) => t.id === id);
    if (!trabajo) return;
    this.revisandoId.set(id);
    this.resultado.set('Conforme');
    this.observaciones.set('');
    this.mensaje.set('');
  }

  cancelarRevision() {
    this.revisandoId.set(null);
    this.mensaje.set('');
  }

  async enviarRevision(id: string) {
    if (this.busy()) return;
    const texto = this.observaciones().trim();
    if (texto.length < 10 || texto.length > 5000) {
      this.mensaje.set('Las observaciones deben tener entre 10 y 5000 caracteres.');
      return;
    }
    const trabajo = this.items().find((t) => t.id === id);
    this.busy.set(true);
    try {
      await this.api.request(`/docente/trabajos/${id}/revisiones`, 'POST', {
        resultado: this.resultado(),
        observaciones: texto,
        entrega_id: trabajo?.id_entrega,
      });
      this.mensaje.set('Revisión registrada correctamente.');
      this.revisandoId.set(null);
      await this.cargar();
    } catch (e: any) {
      this.mensaje.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
