import { Component, DestroyRef, inject, signal } from '@angular/core';
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
  mostrarContrasenas = false;
  paso = signal<'correo' | 'codigo' | 'listo'>('correo');
  busy = signal(false);
  error = signal('');
  message = signal('');
  espera = signal(0);
  private reloj?: ReturnType<typeof setInterval>;
  constructor(){inject(DestroyRef).onDestroy(()=>clearInterval(this.reloj));}
  private iniciarEspera(segundos:number){
    clearInterval(this.reloj);this.espera.set(segundos);
    this.reloj=setInterval(()=>{this.espera.update(v=>Math.max(0,v-1));if(!this.espera())clearInterval(this.reloj);},1000);
  }

  async solicitar(input?: HTMLInputElement) {
    if(this.busy()||this.espera())return;
    if(input){input.value=input.value.trim().toLowerCase();if(!input.reportValidity())return;}
    this.correo=(input?.value??this.correo).trim().toLowerCase();
    await this.ejecutar(async () => {
      const respuesta = await this.api.request<{ mensaje: string; espera_segundos?:number; solicitud_id?:string }>(
        '/auth/recuperacion/solicitar',
        'POST',
        { correo: this.correo },
      );
      this.codigo = '';
      this.mostrarContrasenas = false;
      this.message.set(respuesta.mensaje);
      this.paso.set('codigo');
      this.iniciarEspera(respuesta.espera_segundos??60);
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
      this.mostrarContrasenas = false;
      await this.api.restore();
      this.paso.set('listo');
    });
  }

  volver() {
    this.error.set('');
    this.message.set('');
    this.codigo = this.nueva = this.confirmar = '';
      this.mostrarContrasenas = false;
    this.paso.set('correo');
    clearInterval(this.reloj);this.espera.set(0);
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
