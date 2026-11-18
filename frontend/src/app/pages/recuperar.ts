import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api } from '../core/api';

@Component({
  selector: 'app-recuperar',
  imports: [FormsModule, RouterLink],
  templateUrl: './recuperar.html',
})
export class Recuperar {
  private api = inject(Api);
  correo = this.api.user()?.correo ?? '';
  codigo = '';
  nueva = '';
  confirmar = '';
  paso = signal<'correo' | 'codigo' | 'listo'>('correo');
  busy = signal(false);
  error = signal('');
  message = signal('');

  async solicitar() {
    await this.ejecutar(async () => {
      const respuesta = await this.api.request<{ mensaje: string }>(
        '/auth/recuperacion/solicitar',
        'POST',
        { correo: this.correo },
      );
      this.codigo = '';
      this.message.set(respuesta.mensaje);
      this.paso.set('codigo');
    });
  }

  async recuperar() {
    await this.ejecutar(async () => {
      if (this.nueva !== this.confirmar) throw new Error('Las contraseñas nuevas deben coincidir.');
      await this.api.request('/auth/recuperacion/confirmar', 'POST', {
        correo: this.correo,
        codigo: this.codigo,
        nueva: this.nueva,
      });
      this.codigo = this.nueva = this.confirmar = '';
      await this.api.restore();
      this.paso.set('listo');
    });
  }

  volver() {
    this.error.set('');
    this.message.set('');
    this.codigo = this.nueva = this.confirmar = '';
    this.paso.set('correo');
  }

  private async ejecutar(operacion: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operacion();
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }
}
