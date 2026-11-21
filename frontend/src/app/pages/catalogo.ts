import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api, puedeEnviar, Trabajo } from '../core/api';
import type { OfertaAcademica } from '../shared/carrera-selector';
import type { OpcionCatalogo } from '../core/biblioteca-catalogos';
@Component({
  selector: 'app-catalogo',
  imports: [FormsModule, RouterLink],
  templateUrl: './catalogo.html',
})
export class Catalogo {
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
  async cargarOferta() {
    this.errorOferta.set('');
    try {
      const [oferta, tipos] = await Promise.all([
        this.api.request<OfertaAcademica>('/oferta-academica'),
        this.api.request<OpcionCatalogo[]>('/tipos-de-trabajo'),
      ]);
      this.oferta.set(oferta);
      this.tipos.set(tipos);
    } catch {
      this.errorOferta.set('No se pudo cargar la oferta académica.');
    }
  }
  options(field: 'autores' | 'anio' | 'carrera' | 'area' | 'tipo') {
    const values = this.all().flatMap((work) => {
      if (field === 'autores') return work.autores;
      if (field === 'area') return work.areas ?? [work.area];
      return [String(work[field])];
    });
    return [...new Set(values)].sort();
  }
  constructor() {
    this.load(true);
    if (!this.api.demo) this.cargarOferta();
  }
  async load(initial = false) {
    const consulta = ++this.consultaActual;
    this.loading.set(true);
    this.error.set('');
    try {
      const query = new URLSearchParams({
        q: this.q,
        autor: this.autor,
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
      this.pagina.set(1);
      if (initial || !this.all().length) this.all.set(data);
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
