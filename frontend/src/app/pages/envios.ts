import { Actualizaciones } from '../core/actualizaciones';
import { Transiciones } from '../core/transiciones';
import { MiDocente } from '../shared/mi-docente';
import { Component, computed, inject, signal, viewChild, DestroyRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';
import type { Envio } from '../core/demo/envios';
import { createMetadataDraft, metadataFromDraft, validateMetadata } from '../core/metadata';
import { MetadataFields } from '../shared/metadata-fields';
import { RevisionesDocentes } from '../shared/revisiones-docentes';
import { ArchivosTrabajo } from '../shared/archivos-trabajo';
import { cargarCatalogos, CatalogosBiblioteca, nuevaSeleccion } from '../core/biblioteca-catalogos';

@Component({
  selector: 'app-envios',
  imports: [DatePipe, FormsModule, MetadataFields, ArchivosTrabajo, RouterLink, RevisionesDocentes, MiDocente],
  templateUrl: './envios.html',
})
export class Envios {
 transiciones=inject(Transiciones);
 vivo=inject(Actualizaciones);
  api = inject(Api);
  private metadataFields = viewChild.required(MetadataFields);
  draft = createMetadataDraft(this.api.user()?.nombre);
  invalid = signal<string[]>([]);
  error = signal('');
  listError = signal('');
  message = signal('');
  loading = signal(true);
  busy = signal(false);
  items = signal<Envio[]>([]);
  catalogos = signal<CatalogosBiblioteca | null>(null);
  catalogosError = signal('');
  seleccion = nuevaSeleccion();
  enlaceProyecto = '';

get esProyectoSoftware(): boolean {
  const tipo = this.catalogos()?.tipos.find(
    (t) => Number(t.id) === Number(this.seleccion.id_tipo_trabajo)
  );

  return tipo?.nombre.trim().toLowerCase() === 'proyecto de software';
}
  carreraPropia = computed(() =>
    this.catalogos()?.carreras.find((carrera) => carrera.id === this.api.user()?.id_carrera),
  );

  constructor() {
    this.vivo.watch(['envios'],()=>this.load(true),inject(DestroyRef));
    this.vivo.watch(['catalogos'],()=>this.loadCatalogos(),inject(DestroyRef));
    this.load();
    if (!this.api.demo) this.loadCatalogos();
  }

  async loadCatalogos() {
    this.catalogosError.set('');
    try {
      this.catalogos.set(await cargarCatalogos(this.api));
    } catch (e: any) {
      this.catalogosError.set(e.message);
    }
  }

  async load(background=false) {
    if(!background)this.loading.set(true);
    this.listError.set('');
    try {
      this.items.set(await this.api.request<Envio[]>('/mis-envios'));
    } catch (e: any) {
      this.listError.set(e.message);
    } finally {
      this.loading.set(false);
    }
  }

  async submit(fileInput: HTMLInputElement) {
    if (this.busy()) return;
    this.message.set('');
    this.error.set('');
    const catalogos = this.catalogos();
    if (!this.api.demo && !catalogos) return;
    if (!this.api.demo && !this.carreraPropia()) {
      this.error.set('Completa tu carrera en Mi perfil antes de enviar el trabajo.');
      return;
    }
    const { id_tipo_trabajo, area_ids } = this.seleccion;
    const id_carrera = this.api.demo ? this.seleccion.id_carrera : this.carreraPropia()!.id;
    const metadata = catalogos
      ? {
          ...this.draft,
          autores: [this.api.user()?.nombre ?? ''],
          carrera: catalogos.carreras.find((option) => option.id === id_carrera)?.nombre ?? '',
          tipo: catalogos.tipos.find((option) => option.id === id_tipo_trabajo)?.nombre ?? '',
          area: catalogos.areas
            .filter((option) => area_ids.includes(option.id))
            .map((option) => option.nombre)
            .join('; '),
        }
      : metadataFromDraft(this.draft);
    const invalid = validateMetadata(metadata);
    this.invalid.set(invalid);
    if (invalid.length) {
      this.error.set('Revisa los campos marcados.');
      this.metadataFields().focusFirst(invalid);
      return;
    }
    const esSoftware = this.esProyectoSoftware;
const file = fileInput.files?.[0];
const enlace = this.enlaceProyecto.trim();

if (esSoftware) {
  try {
    const url = new URL(enlace);
    if (url.protocol !== 'https:' || url.username || url.password) throw new Error();
  } catch {
    this.error.set('Ingresa un enlace HTTPS válido para tu proyecto.');
    return;
  }
} else if (!file) {
  this.error.set('Selecciona el PDF de tu trabajo.');
  fileInput.focus();
  return;
}
    const body = new FormData();
    const { autores, titulo, anio, resumen, ...labels } = metadata;
    const fields = this.api.demo
      ? { titulo, anio, resumen, ...labels }
      : { titulo, anio, resumen, id_carrera, id_tipo_trabajo };
    for (const [key, value] of Object.entries(fields)) body.set(key, String(value));
    if (this.api.demo) {
      for (const author of autores) body.append('autores', author);
    } else {
      for (const id of area_ids) body.append('area_ids', String(id));
    }
    if (esSoftware) {
  body.set('enlace', enlace);
} else if (file) {
  body.set('archivo', file);
}

    this.busy.set(true);
    try {
      const submission = await this.api.request<Envio>('/mis-envios', 'POST', body);
      this.items.update((items) => [submission, ...items]);
      this.draft = createMetadataDraft(this.api.user()?.nombre);
      this.seleccion = nuevaSeleccion();
      this.enlaceProyecto = '';
      fileInput.value = '';
      this.message.set('Tu trabajo quedó en revisión en Biblioteca.');
      this.transiciones.confirmar(titulo);
    } catch (e: any) {
      this.error.set(e.message);
      if (Array.isArray(e.details?.fields)) {
        this.invalid.set(e.details.fields);
        this.metadataFields().focusFirst(e.details.fields);
      }
    } finally {
      this.busy.set(false);
    }
  }
}
