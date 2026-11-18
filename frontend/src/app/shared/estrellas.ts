import { Component, computed, input } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { claseValoracion } from '../core/valoraciones';

@Component({
  selector: 'app-estrellas',
  imports: [DecimalPipe],
  template: `
    @if (total()) {
      <span
        class="rating-summary"
        [class]="clase()"
        role="img"
        [attr.aria-label]="media() + ' de 5 estrellas, ' + total() + ' valoraciones'"
      >
        <span class="rating-stars" aria-hidden="true">
          <span>★★★★★</span>
          <span class="rating-stars-fill" [style.width.%]="media() * 20">★★★★★</span>
        </span>
        <span
          >{{ media() | number: '1.1-2' }} · {{ total() }}
          {{ total() === 1 ? 'valoración' : 'valoraciones' }}</span
        >
      </span>
    } @else {
      <span class="muted">Sin valoraciones</span>
    }
  `,
})
export class Estrellas {
  media = input(0);
  total = input(0);
  clase = computed(() => claseValoracion(this.media(), this.total()));
}
