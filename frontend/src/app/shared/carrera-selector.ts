import { Actualizaciones } from '../core/actualizaciones';
import { Component, computed, effect, inject, input, model, signal, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';

export interface DepartamentoAcademico {
  id: number;
  nombre: string;
}
export interface CarreraAcademica {
  id: number;
  nombre: string;
  id_departamento: number;
}
export interface PlanEstudio {
  id: number;
  nombre: string;
  id_carrera: number;
  url?: string | null;
}
export interface OfertaAcademica {
  departamentos: DepartamentoAcademico[];
  carreras: CarreraAcademica[];
  planes: PlanEstudio[];
}

@Component({
  selector: 'app-carrera-selector',
  imports: [FormsModule],
  template: `
    <fieldset>
      <legend>Departamento, carrera y pénsum</legend>
      @if(mostrarResumen()){<dl class="metadata" aria-label="Información académica seleccionada"><div><dt>Departamento</dt><dd>{{departamentoSeleccionado()?.nombre || 'Sin seleccionar'}}</dd></div><div><dt>Carrera</dt><dd>{{carreraSeleccionada()?.nombre || 'Sin seleccionar'}}</dd></div><div><dt>Pénsum</dt><dd>{{planSeleccionado()?.nombre || 'Sin seleccionar'}}</dd></div></dl>}
      <p>
        @if (administracion()) {
          Selecciona el departamento, la carrera y la versión del plan que corresponden a esta cuenta. Su rol se conserva.
        } @else if(conservarRol()) {
          Selecciona tu departamento, carrera y pénsum. Al guardar se conserva tu rol actual.
        } @else {
        Si eres estudiante, elige tu carrera para registrar tu cuenta como Estudiante. Puedes
        continuar como lector dejando la carrera vacía.
        }
      </p>
      @if (error()) {
        <p class="notice error" role="alert">
          {{ error() }}
          <button type="button" class="text-button" (click)="cargar()">Reintentar</button>
        </p>
      }
      <label
        >Departamento
        <select
          [ngModel]="area()"
          (ngModelChange)="cambiarArea($event)"
          [ngModelOptions]="{ standalone: true }"
        >
          <option [ngValue]="null">Todos los departamentos</option>
          @for (opcion of oferta().departamentos; track opcion.id) {
            <option [ngValue]="opcion.id">{{ opcion.nombre }}</option>
          }
        </select>
      </label>
      <label
        >Buscar carrera
        <input
          type="search"
          [ngModel]="busqueda()"
          (ngModelChange)="busqueda.set($event)"
          [ngModelOptions]="{ standalone: true }"
          placeholder="Escribe el nombre de la carrera"
        />
      </label>
      <label
        >Carrera
        <select
          [ngModel]="seleccion()"
          (ngModelChange)="cambiarCarrera($event)"
          [ngModelOptions]="{ standalone: true }"
        >
          <option [ngValue]="null">Continuar sin seleccionar carrera</option>
          @for (opcion of carreras(); track opcion.id) {
            <option [ngValue]="opcion.id">{{ opcion.nombre }}</option>
          }
        </select>
      </label>
      @if (seleccion()) {
        <label
          >Pénsum o plan de estudios
          <select
            [ngModel]="plan()"
            (ngModelChange)="plan.set($event)"
            [ngModelOptions]="{ standalone: true }"
          >
            <option [ngValue]="null">Sin plan seleccionado</option>
            @for (opcion of planes(); track opcion.id) {
              <option [ngValue]="opcion.id">{{ opcion.nombre }}</option>
            }
          </select>
        </label>
        @if (!planes().length) {
          <p>
            No hay versiones de pénsum registradas para esta carrera. Administración puede
            agregarlas.
          </p>
        }
        @if (planSeleccionado()?.url; as url) {
          <p>
            <a [href]="url" target="_blank" rel="noopener noreferrer"
              >Consultar documento del pénsum ↗</a
            >
          </p>
        }
      }
      @if (!carreras().length && oferta().carreras.length) {
        <p role="status">No hay carreras que coincidan con la búsqueda.</p>
      }
    </fieldset>
  `,
})
export class CarreraSelector {
 vivo=inject(Actualizaciones);
  administracion = input(false);
  conservarRol = input(false);
  mostrarResumen = input(false);
  private api = inject(Api);
  seleccion = model<number | null>(null);
  plan = model<number | null>(null);
  area = signal<number | null>(null);
  busqueda = signal('');
  error = signal('');
  oferta = signal<OfertaAcademica>({ departamentos: [], carreras: [], planes: [] });
  carreraSeleccionada = computed(() => this.oferta().carreras.find(c => c.id === this.seleccion()));
  departamentoSeleccionado = computed(() => this.oferta().departamentos.find(d => d.id === this.carreraSeleccionada()?.id_departamento));
  planes = computed(() => this.oferta().planes.filter((p) => p.id_carrera === this.seleccion()));
  planSeleccionado = computed(() => this.planes().find((p) => p.id === this.plan()));
  carreras = computed(() => {
    const texto = this.normalizar(this.busqueda());
    return this.oferta().carreras.filter(
      (c) =>
        (this.area() === null || c.id_departamento === this.area()) &&
        (c.id === this.seleccion() || this.normalizar(c.nombre).includes(texto)),
    );
  });
  constructor() {
    this.vivo.watch(['catalogos'],()=>this.cargar(),inject(DestroyRef));
    this.cargar();
    effect(() => {
      const elegida = this.oferta().carreras.find((c) => c.id === this.seleccion());
      if (elegida) this.area.set(elegida.id_departamento);
    });
  }
  private normalizar(texto: string) {
    return texto
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }
  cambiarArea(area: number | null) {
    this.seleccion.set(null);
    this.plan.set(null);
    this.area.set(area);
    this.busqueda.set('');
  }
  cambiarCarrera(carrera: number | null) {
    this.seleccion.set(carrera);
    this.plan.set(null);
  }
  async cargar() {
    this.error.set('');
    try {
      this.oferta.set(await this.api.request<OfertaAcademica>('/oferta-academica'));
    } catch {
      this.error.set('No se pudo cargar la oferta académica.');
    }
  }
}
