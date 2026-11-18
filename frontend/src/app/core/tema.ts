import { DOCUMENT } from '@angular/common';
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { Api, Tema } from './api';

const key = 'scriptorium.tema';

@Injectable({ providedIn: 'root' })
export class PreferenciaTema {
  private api = inject(Api);
  private doc = inject(DOCUMENT);
  private media = window.matchMedia('(prefers-color-scheme: dark)');
  private systemDark = signal(this.media.matches);
  private local = signal<Tema>(this.read());
  readonly preferencia = computed(() => this.api.user()?.tema ?? this.local());
  readonly oscuro = computed(() =>
    this.preferencia() === 'sistema' ? this.systemDark() : this.preferencia() === 'oscuro',
  );

  constructor() {
    this.media.addEventListener('change', (event) => this.systemDark.set(event.matches));
    effect(() => {
      const saved = this.api.user()?.tema;
      if (saved) this.local.set(saved);
      this.doc.documentElement.dataset['theme'] = this.oscuro() ? 'dark' : 'light';
      try {
        localStorage.setItem(key, this.preferencia());
      } catch {}
    });
  }

  async set(tema: Tema) {
    if (this.api.user()) await this.api.updateProfile({ tema });
    this.local.set(tema);
  }

  private read(): Tema {
    try {
      const value = localStorage.getItem(key);
      if (value === 'claro' || value === 'oscuro') return value;
    } catch {}
    return 'sistema';
  }
}
