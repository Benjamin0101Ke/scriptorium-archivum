import { Component, inject, input, model, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Api } from '../core/api';
import { OpcionCatalogo } from '../core/biblioteca-catalogos';

@Component({
  selector: 'app-person-picker',
  imports: [FormsModule],
  templateUrl: './person-picker.html',
})
export class PersonPicker {
  private api = inject(Api);
  searchId = input.required<string>();
  label = input.required<string>();
  invalid = input(false);
  selected = model<OpcionCatalogo[]>([]);
  busy = signal(false);
  error = signal('');
  results = signal<OpcionCatalogo[] | null>(null);
  query = '';

  async search() {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.results.set(null);
    try {
      const query = new URLSearchParams({ q: this.query.trim() });
      this.results.set(await this.api.request<OpcionCatalogo[]>('/admin/personas?' + query));
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }

  add(person: OpcionCatalogo) {
    if (!this.selected().some((item) => item.id === person.id))
      this.selected.update((items) => [...items, person]);
  }

  remove(id: number) {
    this.selected.update((items) => items.filter((item) => item.id !== id));
  }

  isSelected(id: number) {
    return this.selected().some((item) => item.id === id);
  }
}
