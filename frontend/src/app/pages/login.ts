import { Component, ElementRef, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api, puedeEnviar } from '../core/api';
import { espaciosPorRol } from '../core/roles';
import { CarreraSelector } from '../shared/carrera-selector';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, CarreraSelector],
  templateUrl: './login.html',
})
export class Login {
  api = inject(Api);
  router = inject(Router);
  private route = inject(ActivatedRoute);
  private element = inject<ElementRef<HTMLElement>>(ElementRef);
  nombre = '';
  apellidos = '';
  correo = '';
  password = '';
  carrera: number | null = null;
  plan: number | null = null;
  registering = signal(false);
  error = signal('');
  busy = signal(false);
  showPassword = signal(false);
  togglePassword() {
    this.showPassword.update((value) => !value);
  }
  forSubmissions() {
    return this.route.snapshot.queryParamMap.get('destino') === 'envios';
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
      if (this.registering())
        await this.api.register(
          this.nombre,
          this.apellidos,
          this.correo,
          this.password,
          this.carrera,
          this.plan,
        );
      else await this.api.login(this.correo, this.password);
      if (!this.registering() && this.carrera && puedeEnviar(this.api.user()?.rol))
        await this.api.updateProfile({ id_carrera: this.carrera, id_plan: this.plan });
      const rol = this.api.user()?.rol;
      if (this.forSubmissions() && puedeEnviar(rol)) {
        await this.router.navigateByUrl('/mis-envios');
      } else {
        await this.router.navigateByUrl(rol ? '/' + espaciosPorRol[rol].ruta : '/');
      }
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
