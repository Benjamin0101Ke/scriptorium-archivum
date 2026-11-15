import { Component, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';
import type { Envio } from '../core/demo/envios';
import { createMetadataDraft, metadataFromDraft, validateMetadata } from '../core/metadata';
import { MetadataFields } from '../shared/metadata-fields';

@Component({
  selector: 'app-envios',
  imports: [DatePipe, FormsModule, MetadataFields],
  templateUrl: './envios.html',
})
export class Envios {
  private api = inject(Api);
  private metadataFields = viewChild.required(MetadataFields);
  draft = createMetadataDraft(this.api.user()?.nombre);
  invalid = signal<string[]>([]);
  error = signal('');
  listError = signal('');
  message = signal('');
  loading = signal(true);
  busy = signal(false);
  items = signal<Envio[]>([]);

  constructor() {
    this.load();
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
    const metadata = metadataFromDraft(this.draft);
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
    const { autores, ...fields } = metadata;
    for (const [key, value] of Object.entries(fields)) body.set(key, String(value));
    for (const author of autores) body.append('autores', author);
    body.set('archivo', file);

    this.busy.set(true);
    try {
      const submission = await this.api.request<Envio>('/mis-envios', 'POST', body);
      this.items.update((items) => [submission, ...items]);
      this.draft = createMetadataDraft(this.api.user()?.nombre);
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
