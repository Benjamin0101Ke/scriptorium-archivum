import { Component, ElementRef, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { MetadataDraft } from '../core/metadata';

@Component({
  selector: 'app-metadata-fields',
  imports: [FormsModule],
  templateUrl: './metadata-fields.html',
})
export class MetadataFields {
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  draft = input.required<MetadataDraft>();
  invalidFields = input<string[]>([]);
  readonly fields: { name: keyof MetadataDraft; label: string }[] = [
    { name: 'titulo', label: 'Título' },
    { name: 'autores', label: 'Autores, separados por punto y coma' },
    { name: 'anio', label: 'Año' },
    { name: 'carrera', label: 'Carrera' },
    { name: 'area', label: 'Área' },
    { name: 'tipo', label: 'Tipo de trabajo' },
    { name: 'resumen', label: 'Resumen' },
  ];

  focusFirst(fields: string[] = ['titulo']): void {
    const first = this.fields.find((field) => fields.includes(field.name));
    if (first) this.host.nativeElement.querySelector<HTMLElement>('#meta-' + first.name)?.focus();
  }
}
