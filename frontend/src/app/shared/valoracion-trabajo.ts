import { Component, computed, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api, Trabajo } from '../core/api';
import { Estrellas } from './estrellas';

@Component({
  selector: 'app-valoracion-trabajo',
  imports: [RouterLink, Estrellas],
  template: `
    <section class="panel rating-panel">
      <h2>Valoraciones del trabajo</h2>
      <app-estrellas
        [media]="trabajo().valoracion_media ?? 0"
        [total]="trabajo().valoraciones_total ?? 0"
      />
      @if (api.user()) {
        <p>Tu valoración</p>
        <div class="rating-input" role="group" aria-label="Tu valoración">
          @for (numero of opciones; track numero) {
            <button
              type="button"
              class="rating-choice"
              [class.selected]="seleccion() >= numero"
              [attr.aria-pressed]="seleccion() === numero"
              [attr.aria-label]="
                'Valorar con ' + numero + (numero === 1 ? ' estrella' : ' estrellas')
              "
              (click)="seleccion.set(numero)"
              [disabled]="ocupado()"
            >
              ★
            </button>
          }
        </div>
        <p class="muted">Una valoración por cuenta. Podés actualizarla cuando quieras.</p>
        <div class="actions">
          <button type="button" (click)="guardar()" [disabled]="ocupado() || !seleccion()">
            Guardar valoración
          </button>
          @if (guardada()) {
            <button type="button" class="secondary" (click)="quitar()" [disabled]="ocupado()">
              Quitar mi valoración
            </button>
          }
        </div>
      } @else {
        <p><a routerLink="/ingresar">Iniciá sesión para valorar este trabajo</a></p>
      }
      @if (error()) {
        <p class="notice error" role="alert">{{ error() }}</p>
      }
      @if (message()) {
        <p class="notice" role="status">{{ message() }}</p>
      }
    </section>
  `,
})
export class ValoracionTrabajo {
  private destroyRef = inject(DestroyRef);
  api = inject(Api);
  trabajo = input.required<Trabajo>();
  actualizado = output<Trabajo>();
  private id = computed(() => this.trabajo().id);
  seleccion = signal(0);
  guardada = signal(0);
  error = signal('');
  message = signal('');
  private loading = signal(false);
  private saving = signal(false);
  ocupado = computed(() => this.loading() || this.saving());
  readonly opciones = [1, 2, 3, 4, 5];

  constructor() {
    effect(() => {
      const id = this.id();
      const usuario = this.api.user()?.id;
      this.seleccion.set(0);
      this.guardada.set(0);
      this.error.set('');
      this.message.set('');
      if (usuario) this.cargar(id, usuario);
    });
  }

  private async cargar(id: string, usuario: string) {
    this.loading.set(true);
    try {
      const data = await this.api.request<{ estrellas: number | null }>(
        '/trabajos/' + id + '/valoracion',
      );
      if (this.destroyRef.destroyed || id !== this.id() || usuario !== this.api.user()?.id) return;
      this.seleccion.set(data.estrellas ?? 0);
      this.guardada.set(data.estrellas ?? 0);
    } catch (e: any) {
      if (!this.destroyRef.destroyed && id === this.id()) this.error.set(e.message);
    } finally {
      if (!this.destroyRef.destroyed && id === this.id()) this.loading.set(false);
    }
  }

  guardar() {
    return this.enviar('PUT');
  }
  quitar() {
    return this.enviar('DELETE');
  }

  private async enviar(metodo: 'PUT' | 'DELETE') {
    if (this.ocupado()) return;
    const id = this.id();
    const seleccion = this.seleccion();
    this.saving.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await this.api.request(
        '/trabajos/' + id + '/valoracion',
        metodo,
        metodo === 'PUT' ? { estrellas: seleccion } : undefined,
      );
      const trabajo = await this.api.request<Trabajo>('/trabajos/' + id);
      if (this.destroyRef.destroyed || id !== this.id()) return;
      this.seleccion.set(metodo === 'PUT' ? seleccion : 0);
      this.guardada.set(metodo === 'PUT' ? seleccion : 0);
      this.actualizado.emit(trabajo);
      this.message.set(metodo === 'PUT' ? 'Valoración guardada.' : 'Valoración eliminada.');
    } catch (e: any) {
      if (!this.destroyRef.destroyed && id === this.id()) this.error.set(e.message);
    } finally {
      if (!this.destroyRef.destroyed) this.saving.set(false);
    }
  }
}
