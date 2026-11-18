import { Component, computed, ElementRef, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { MetadataDraft } from '../core/metadata';
import type { CatalogosBiblioteca, SeleccionBiblioteca } from '../core/biblioteca-catalogos';
import { PersonPicker } from './person-picker';

@Component({
  selector: 'app-metadata-fields',
  imports: [FormsModule, PersonPicker],
  templateUrl: './metadata-fields.html',
})
export class MetadataFields {
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  draft = input.required<MetadataDraft>();
  catalogos = input<CatalogosBiblioteca | null>(null);
  seleccion = input<SeleccionBiblioteca | null>(null);
  autorPropio = input<string | null>(null);
  carreraPropia = input<string | null>(null);
  invalidFields = input<string[]>([]);
  tipoFueraCatalogo = computed(() => {
    const id = this.seleccion()?.id_tipo_trabajo;
    const catalogo = this.catalogos();
    return !!id && !!catalogo && !catalogo.tipos.some((tipo) => tipo.id === id);
  });
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
