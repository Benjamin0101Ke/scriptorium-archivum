import { Actualizaciones } from '../core/actualizaciones';
import { AbrirConEfecto } from '../core/transiciones';
import { SelectorBuscable } from '../shared/selector-buscable';
import { Component, computed, inject, signal, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api, puedeEnviar, Trabajo } from '../core/api';
import type { OfertaAcademica } from '../shared/carrera-selector';
import type { OpcionCatalogo } from '../core/biblioteca-catalogos';
@Component({
  selector: 'app-catalogo',
  imports: [FormsModule, RouterLink, AbrirConEfecto,SelectorBuscable],
  templateUrl: './catalogo.html',
})
export class Catalogo {
 vivo=inject(Actualizaciones);
  puedeEnviar = puedeEnviar;
  api = inject(Api);
  items = signal<Trabajo[]>([]);
  all = signal<Trabajo[]>([]);
  loading = signal(true);
  error = signal('');
  q = '';
  autor = '';
  anio = '';
  carrera = '';
  area = '';
  tipo = '';
  orden = 'coincidencia';
  departamento = '';
  oferta = signal<OfertaAcademica>({ departamentos: [], carreras: [], planes: [] });
  errorOferta = signal('');
  tipos = signal<OpcionCatalogo[]>([]);
  areas = signal<OpcionCatalogo[]>([]);
  areasFiltro = computed(() => [...new Set([
    ...this.areas().map((opcion) => opcion.nombre),
    ...this.all().flatMap((trabajo) => trabajo.areas ?? [trabajo.area]),
  ])].filter((nombre) => nombre?.trim()).sort((a,b) => a.localeCompare(b,'es')));
  tiposFiltro = computed(() =>
    [
      ...new Set([
        ...this.tipos().map((tipo) => tipo.nombre),
        ...this.all().map((trabajo) => trabajo.tipo),
      ]),
    ].sort(),
  );
  readonly tamanoPagina = 12;
  pagina = signal(1);
  totalPaginas = computed(() => Math.ceil(this.items().length / this.tamanoPagina));
  inicioPagina = computed(() => (this.pagina() - 1) * this.tamanoPagina);
  finPagina = computed(() =>
    Math.min(this.inicioPagina() + this.tamanoPagina, this.items().length),
  );
  trabajosPagina = computed(() => this.items().slice(this.inicioPagina(), this.finPagina()));
  paginasVisibles = computed(() => {
    const total = this.totalPaginas();
    const actual = this.pagina();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const paginas = [...new Set([1, actual - 1, actual, actual + 1, total])]
      .filter((pagina) => pagina >= 1 && pagina <= total)
      .sort((a, b) => a - b);
    const controles: (number | null)[] = [];
    for (const [indice, pagina] of paginas.entries()) {
      if (indice > 0 && pagina - paginas[indice - 1] > 1) controles.push(null);
      controles.push(pagina);
    }
    return controles;
  });
  private consultaActual = 0;
  cambiarPagina(pagina: number, resumen: HTMLElement) {
    if (pagina === this.pagina() || pagina < 1 || pagina > this.totalPaginas()) return;
    this.pagina.set(pagina);
    resumen.focus({ preventScroll: true });
    resumen.scrollIntoView({
      behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
      block: 'start',
    });
  }
  carrerasAcademicas() {
    return this.oferta().carreras.filter(
      (c) => !this.departamento || String(c.id_departamento) === this.departamento,
    );
  }
  opcionesCarrera(){return this.api.demo?this.options('carrera'):this.carrerasAcademicas().map(c=>c.nombre);}
  async cargarOferta() {
    this.errorOferta.set('');
    try {
      const [oferta, tipos, areas] = await Promise.all([
        this.api.request<OfertaAcademica>('/oferta-academica'),
        this.api.request<OpcionCatalogo[]>('/tipos-de-trabajo'),
        this.api.request<OpcionCatalogo[]>('/areas-tematicas'),
      ]);
      this.oferta.set(oferta);
      this.tipos.set(tipos);
      this.areas.set(areas);
    } catch {
      this.errorOferta.set('No se pudo cargar la oferta académica.');
    }
  }
  options(field: 'anio' | 'carrera' | 'area' | 'tipo') {
    const values = this.all().flatMap((work) => {
      if (field === 'area') return work.areas ?? [work.area];
      return [String(work[field])];
    });
    return [...new Set(values)].sort();
  }
  constructor() {
    this.vivo.watch(['catalogo'],()=>this.load(false,true),inject(DestroyRef));
    this.vivo.watch(['catalogos'],()=>this.cargarOferta(),inject(DestroyRef));
    this.load(true);
    if (!this.api.demo) this.cargarOferta();
  }
  async load(initial = false, background = false) {
    const consulta = ++this.consultaActual;
    if(!background)this.loading.set(true);
    this.error.set('');
    try {
      const query = new URLSearchParams({
        q: this.q,
        autor: this.autor.trim(),
        anio: this.anio,
        carrera: this.carrera,
        area: this.area,
        tipo: this.tipo,
        orden: this.orden,
      });
      if (this.departamento) query.set('departamento', this.departamento);
      const data = await this.api.request<Trabajo[]>('/trabajos?' + query);
      if (consulta !== this.consultaActual) return;
      this.items.set(data);
      this.pagina.set(background ? Math.min(this.pagina(),Math.max(1,this.totalPaginas())) : 1);
      if (initial || !this.all().length || !(this.q||this.autor||this.anio||this.carrera||this.area||this.tipo||this.departamento)) this.all.set(data);
    } catch (e: any) {
      if (consulta === this.consultaActual) this.error.set(e.message);
    } finally {
      if (consulta === this.consultaActual) this.loading.set(false);
    }
  }
  reset() {
    this.q = this.autor = this.anio = this.carrera = this.area = this.tipo = this.departamento = '';
    this.orden = 'coincidencia';
    this.load();
  }
}
