import { Component, ElementRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Api } from '../core/api';

@Component({ selector: 'app-login', imports: [FormsModule], templateUrl: './login.html' })
export class Login {
  api = inject(Api);
  router = inject(Router);
  private route = inject(ActivatedRoute);
  private element = inject<ElementRef<HTMLElement>>(ElementRef);
  nombre = '';
  correo = '';
  password = '';
  registering = signal(false);
  error = signal('');
  busy = signal(false);

  forSubmissions() {
    return this.api.demo && this.route.snapshot.queryParamMap.get('destino') === 'envios';
  }

  toggleRegistration() {
    this.registering.update((value) => !value);
    this.password = '';
    this.error.set('');
  }

  async submit() {
    if (this.busy()) return;
    const invalid =
      this.element.nativeElement.querySelector<HTMLInputElement>('form input:invalid');
    if (invalid) {
      invalid.reportValidity();
      return;
    }
    this.busy.set(true);
    this.error.set('');
    try {
      if (this.registering()) await this.api.registerLocal(this.nombre, this.correo, this.password);
      else await this.api.login(this.correo, this.password);
      await this.router.navigateByUrl(
        this.api.user()?.rol === 'Biblioteca'
          ? '/biblioteca'
          : this.forSubmissions()
            ? '/mis-envios'
            : '/favoritos',
      );
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
