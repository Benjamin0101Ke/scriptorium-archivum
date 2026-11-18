import { Component, input } from '@angular/core';
import type { Avatar as AvatarTipo } from '../core/api';

@Component({
  selector: 'app-avatar',
  host: { '[class.avatar-violeta]': "tipo() === 'violeta'", 'aria-hidden': 'true' },
  template: `
    @if (foto()) {
      <img [src]="foto()" alt="" style="width:100%;height:100%;object-fit:cover;border-radius:inherit" />
    } @else {
    <svg
      viewBox="0 0 32 32"
      fill="none"
      stroke="currentColor"
      stroke-width="2.05"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      <circle cx="16" cy="8" r="3.5"></circle>
      @if (tipo() === 'violeta') {
        <path d="m9 19 7-5 7 5m-7-5-4 10h8l-4-10m-3 10-1 4m7-4 1 4"></path>
      } @else {
        <path d="m9 19 7-5 7 5m-7-5v7m0 0-4 7m4-7 4 7"></path>
      }
    </svg>
    }
  `,
})
export class Avatar {
  tipo = input<AvatarTipo | undefined>('turquesa');
  foto = input<string|null|undefined>(null);
}
