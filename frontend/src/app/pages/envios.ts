import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';
import type { Envio } from '../core/demo/envios';
import { createMetadataDraft, metadataFromDraft, validateMetadata } from '../core/metadata';
import { MetadataFields } from '../shared/metadata-fields';
import { ArchivosTrabajo } from '../shared/archivos-trabajo';
import { cargarCatalogos, CatalogosBiblioteca, nuevaSeleccion } from '../core/biblioteca-catalogos';

@Component({
  selector: 'app-envios',
  imports: [DatePipe, FormsModule, MetadataFields, ArchivosTrabajo, RouterLink],
  templateUrl: './envios.html',
})
export class Envios {
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
  carreraPropia = computed(() =>
    this.catalogos()?.carreras.find((carrera) => carrera.id === this.api.user()?.id_carrera),
  );

  constructor() {
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

  async load() {
    this.loading.set(true);
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
      this.error.set('Completá tu carrera en Mi perfil antes de enviar el trabajo.');
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
      this.error.set('Revisá los campos marcados.');
      this.metadataFields().focusFirst(invalid);
      return;
    }
    const file = fileInput.files?.[0];
    if (!file) {
      this.error.set('Seleccioná el PDF de tu trabajo.');
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
    body.set('archivo', file);

    this.busy.set(true);
    try {
      const submission = await this.api.request<Envio>('/mis-envios', 'POST', body);
      this.items.update((items) => [submission, ...items]);
      this.draft = createMetadataDraft(this.api.user()?.nombre);
      this.seleccion = nuevaSeleccion();
      fileInput.value = '';
      this.message.set('Tu trabajo quedó en revisión en Biblioteca.');
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
