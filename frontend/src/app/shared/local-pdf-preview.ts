import { Component, computed, DestroyRef, inject, input, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { Api } from '../core/api';

@Component({
  selector: 'app-local-pdf-preview',
  template: `
    <div class="pdf-preview">
      @if (visiblePreview(); as current) {
        <button type="button" class="secondary" (click)="close()">Cerrar vista previa</button>
        <iframe [src]="current.safeUrl" title="PDF para revisión"></iframe>
      } @else {
        <button type="button" class="secondary" (click)="open()" [disabled]="busy()">
          Revisar PDF
        </button>
      }
      @if (error()) {
        <p class="notice error" role="alert">{{ error() }}</p>
      }
    </div>
  `,
})
export class LocalPdfPreview {
  private api = inject(Api);
  private sanitizer = inject(DomSanitizer);
  private destroyRef = inject(DestroyRef);
  path = input.required<string>();
  busy = signal(false);
  error = signal('');
  private preview = signal<{ path: string; url: string; safeUrl: SafeResourceUrl } | null>(null);
  visiblePreview = computed(() => {
    const current = this.preview();
    return current?.path === this.path() ? current : null;
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.close());
  }

  close() {
    const current = this.preview();
    if (current) URL.revokeObjectURL(current.url);
    this.preview.set(null);
  }

  async open() {
    if (this.busy()) return;
    const path = this.path();
    this.error.set('');
    this.busy.set(true);
    try {
      const file = await this.api.pdf(path);
      if (this.destroyRef.destroyed || path !== this.path()) return;
      this.close();
      const url = URL.createObjectURL(file);
      this.preview.set({ path, url, safeUrl: this.sanitizer.bypassSecurityTrustResourceUrl(url) });
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
