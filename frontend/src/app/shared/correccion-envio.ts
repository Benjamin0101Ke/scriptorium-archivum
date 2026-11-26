import {Component, DestroyRef, effect, inject, input, signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Api} from '../core/api';
import {Actualizaciones} from '../core/actualizaciones';

interface EstadoCorreccion {
  ejemplar_id: string | null;
  version: number | null;
  es_software: boolean;
  constancia_revocada: boolean;
  decision: string | null;
  motivo_biblioteca: string | null;
  revision_docente: string | null;
  motivo_docente: string | null;
  puede_reemplazar: boolean;
}

@Component({
  selector: 'app-correccion-envio',
  imports: [FormsModule],
  template: `
    @if (estado(); as e) {
      @if(e.version){<p class="muted">Versión actual: {{e.version}}</p>}
      @if(e.decision === 'Desfavorable') {
        <p class="notice">Biblioteca solicita revisión: {{e.motivo_biblioteca}}</p>
      }
      @if(e.revision_docente === 'Requiere cambios') {
        <p class="notice">Cambios solicitados por tu docente: {{e.motivo_docente}}</p>
      }
      @if(e.constancia_revocada) {
        <p class="notice">La constancia de autorización está revocada. Enviar otro documento no restablece la autorización; Biblioteca debe registrar una constancia vigente antes de publicarlo.</p>
      }
      @if(e.puede_reemplazar && !abierto()) {
        <button type="button" class="secondary" data-icon="upload" (click)="abrir()" [disabled]="busy()">Enviar versión corregida</button>
      }
      @if(abierto()) {
        <form #correctionForm class="panel" (ngSubmit)="submit(correctionForm)" [attr.aria-busy]="busy()">
          <h3>Enviar versión corregida</h3>
          <p>Se conservará el documento anterior. La nueva versión quedará en revisión y no se publicará automáticamente.</p>
          @if(e.es_software) {
            <label>Enlace actualizado del proyecto
              <input name="enlace" type="url" [(ngModel)]="enlace" required maxlength="2048" placeholder="https://…" [disabled]="busy()" />
            </label>
          } @else {
            <label>PDF corregido · máximo 6 GB
              <input name="archivo" type="file" accept="application/pdf,.pdf" required [disabled]="busy()" />
            </label>
          }
          <label>Cambios realizados
            <textarea name="cambios" [(ngModel)]="cambios" required minlength="10" maxlength="1500" rows="3" [disabled]="busy()" placeholder="Explica cómo atendiste las observaciones recibidas."></textarea>
          </label>
          <div class="actions">
            <button data-icon="send" [disabled]="busy()">{{busy() ? 'Enviando versión…' : 'Enviar nueva versión a revisión'}}</button>
            <button type="button" class="secondary" (click)="abierto.set(false)" [disabled]="busy()">Cancelar</button>
          </div>
        </form>
      }
    }
    @if(error()){<p class="notice error" role="alert">{{error()}}</p>}
    @if(loadError()) {
      <p class="muted" role="status">No se pudo consultar si el trabajo requiere correcciones.</p>
      <button type="button" class="secondary" (click)="load()">Reintentar</button>
    }
    @if(message()){<p class="notice" role="status">{{message()}}</p>}
  `,
  styles: `:host{display:block;flex-basis:100%;min-width:0}form{margin-top:14px}h3{margin-top:0}.notice{overflow-wrap:anywhere}button{white-space:normal}`,
})
export class CorreccionEnvio {
  trabajo = input.required<string>();
  private api = inject(Api);
  private vivo = inject(Actualizaciones);
  estado = signal<EstadoCorreccion | null>(null);
  abierto = signal(false);
  busy = signal(false);
  error = signal('');
  loadError = signal(false);
  message = signal('');
  cambios = '';
  enlace = '';
  private consulta = 0;

  constructor() {
    effect(() => { const id = this.trabajo(); void this.load(id); });
    this.vivo.watch(['envios'], () => this.load(), inject(DestroyRef));
  }

  async load(id = this.trabajo()) {
    if (this.busy()) return;
    const consulta = ++this.consulta;
    try {
      const estado = await this.api.request<EstadoCorreccion>('/mis-envios/' + id + '/correccion');
      if (consulta !== this.consulta) return;
      this.estado.set(estado);
      this.loadError.set(false);
    } catch {
      if (consulta === this.consulta) this.loadError.set(true);
    }
  }

  abrir() {
    this.error.set('');
    this.message.set('');
    this.cambios = '';
    this.enlace = '';
    this.abierto.set(true);
  }

  async submit(form: HTMLFormElement) {
    if (this.busy()) return;
    const estado = this.estado();
    if (!estado?.puede_reemplazar || !estado.ejemplar_id) return;
    this.error.set('');
    this.message.set('');
    const cambios = this.cambios.trim();
    if(cambios.length < 10 || cambios.length > 1500) {
      this.error.set('Describe los cambios realizados, entre 10 y 1500 caracteres.');
      return;
    }
    const body = new FormData();
    body.set('ejemplar_id', estado.ejemplar_id);
    body.set('cambios', cambios);
    if (estado.es_software) {
      try {
        const url = new URL(this.enlace.trim());
        if (url.protocol !== 'https:' || url.username || url.password) throw new Error();
      } catch { this.error.set('Ingresa un enlace HTTPS válido, sin usuario ni contraseña en el enlace.'); return; }
      body.set('enlace', this.enlace.trim());
    } else {
      const file = form.querySelector<HTMLInputElement>('input[type="file"]')?.files?.[0];
      if (!file || !file.name.toLowerCase().endsWith('.pdf') || file.size < 5 || file.size > 6 * 1024 ** 3) {
        this.error.set('Selecciona el PDF corregido, de hasta 6 GB.'); return;
      }
      body.set('archivo', file);
    }
    this.busy.set(true);
    ++this.consulta;
    try {
      const resultado = await this.api.request<{version:number}>('/mis-envios/' + this.trabajo() + '/reemplazo', 'POST', body);
      this.abierto.set(false);
      this.message.set('La versión ' + resultado.version + ' quedó en revisión. Se notificó el nuevo envío a Biblioteca y a tu docente, si tienes uno asignado.');
    } catch(e: any) {
      this.error.set(e.message || 'No se pudo enviar la versión corregida.');
    } finally {
      this.busy.set(false);
      await this.load();
    }
  }
}
