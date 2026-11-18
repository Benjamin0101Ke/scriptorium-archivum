import { Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';

interface ArchivoTrabajo {
  id: string;
  nombre: string;
  descripcion: string;
  tamano_bytes: number;
  habilitado: boolean;
}

@Component({
  selector: 'app-archivos-trabajo',
  imports: [FormsModule],
  template: `
    <section class="panel">
      <h2>Archivos complementarios</h2>
      @if (editable()) {
        <label
          >Archivo Word, Excel, PDF, ZIP, código o texto · máximo 25 MB
          <input
            #archivo
            type="file"
            accept=".pdf,.doc,.docx,.xls,.xlsx,.zip,.txt,.csv,.md,.js,.ts,.html,.css,.json,.py,.java,.cs,.sql,.cpp,.c,.h"
            [disabled]="busy()"
          />
        </label>
        <label
          >Descripción
          <input
            [ngModel]="descripcion"
            (ngModelChange)="descripcion = $event"
            [ngModelOptions]="{ standalone: true }"
            maxlength="1500"
          />
        </label>
        <button type="button" (click)="subir(archivo)" [disabled]="busy()">Adjuntar archivo</button>
        <p>
          Biblioteca habilita cada archivo para su difusión. La descarga pública requiere el permiso
          de descarga del trabajo.
        </p>
      }
      @if (error()) {
        <p class="notice error" role="alert">{{ error() }}</p>
      }
      @if (message()) {
        <p class="notice" role="status">{{ message() }}</p>
      }
      @for (archivo of archivos(); track archivo.id) {
        <article>
          <h3>{{ archivo.nombre }}</h3>
          <p>{{ archivo.descripcion }} · {{ tamano(archivo.tamano_bytes) }}</p>
          <div class="actions">
            <a
              class="button secondary"
              [href]="'/api/trabajos/' + trabajo() + '/archivos/' + archivo.id"
              >Descargar {{ archivo.nombre }}</a
            >
            @if (editable()) {
              <span>{{
                archivo.habilitado
                  ? 'Difusión habilitada'
                  : 'Pendiente de autorización de Biblioteca'
              }}</span>
            }
            @if (biblioteca()) {
              <button
                type="button"
                class="secondary"
                (click)="habilitar(archivo)"
                [disabled]="busy()"
              >
                {{ archivo.habilitado ? 'Suspender difusión' : 'Habilitar difusión' }} ·
                {{ archivo.nombre }}
              </button>
            }
          </div>
        </article>
      }
      @if (!archivos().length && !busy() && !error()) {
        <p>No hay archivos complementarios disponibles.</p>
      }
    </section>
  `,
})
export class ArchivosTrabajo {
  tamano(bytes: number) {
    if (bytes < 1024) return bytes + ' B';
    return (bytes / 1024).toFixed(1) + ' KB';
  }
  private api = inject(Api);
  trabajo = input.required<string>();
  editable = input(false);
  biblioteca = input(false);
  archivos = signal<ArchivoTrabajo[]>([]);
  error = signal('');
  message = signal('');
  busy = signal(false);
  descripcion = '';
  constructor() {
    effect(() => {
      const id = this.trabajo();
      this.descripcion = '';
      this.cargar(id);
    });
  }
  private async cargar(id: string) {
    this.error.set('');
    this.message.set('');
    this.archivos.set([]);
    try {
      const archivos = await this.api.request<ArchivoTrabajo[]>('/trabajos/' + id + '/archivos');
      if (id === this.trabajo()) this.archivos.set(archivos);
    } catch (e: any) {
      if (id === this.trabajo()) this.error.set(e.message);
    }
  }
  async subir(input: HTMLInputElement) {
    if (this.busy()) return;
    this.error.set('');
    this.message.set('');
    const file = input.files?.[0];
    if (!file || file.size > 25 * 1024 * 1024) {
      this.error.set('Seleccioná un archivo de hasta 25 MB.');
      return;
    }
    if (!this.descripcion.trim()) {
      this.error.set('Escribí la descripción del archivo.');
      return;
    }
    const id = this.trabajo();
    this.busy.set(true);
    try {
      const body = new FormData();
      body.set('archivo', file);
      body.set('descripcion', this.descripcion.trim());
      await this.api.request('/trabajos/' + id + '/archivos', 'POST', body);
      if (id !== this.trabajo()) return;
      input.value = '';
      this.descripcion = '';
      await this.cargar(id);
      this.message.set('Archivo guardado para revisión de Biblioteca.');
    } catch (e: any) {
      if (id === this.trabajo()) this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
  async habilitar(archivo: ArchivoTrabajo) {
    if (this.busy()) return;
    const id = this.trabajo();
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await this.api.request('/trabajos/' + id + '/archivos/' + archivo.id, 'PATCH', {
        habilitado: !archivo.habilitado,
      });
      if (id === this.trabajo()) {
        await this.cargar(id);
        this.message.set('Permiso del archivo actualizado.');
      }
    } catch (e: any) {
      if (id === this.trabajo()) this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
