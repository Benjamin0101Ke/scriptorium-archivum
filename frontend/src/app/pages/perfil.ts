import { Actualizaciones } from '../core/actualizaciones';
import { MiDocente } from '../shared/mi-docente';
import { Component, inject, signal, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Api, Avatar as AvatarTipo, Tema } from '../core/api';
import { PreferenciaTema } from '../core/tema';
import { Avatar } from '../shared/avatar';
import { CarreraSelector } from '../shared/carrera-selector';

@Component({
  selector: 'app-perfil',
  imports: [FormsModule, RouterLink, Avatar, CarreraSelector, MiDocente],
  templateUrl: './perfil.html',
})
export class Perfil {
 vivo=inject(Actualizaciones);
  api = inject(Api);
  private tema = inject(PreferenciaTema);
  nombre = this.api.user()?.nombre ?? '';
  avatar: AvatarTipo = this.api.user()?.avatar ?? 'turquesa';
  temaElegido: Tema = this.tema.preferencia();
  carrera: number | null = this.api.user()?.id_carrera ?? null;
  plan: number | null = this.api.user()?.id_plan ?? null;
  actual = '';
  nueva = '';
  confirmar = '';
  busy = signal(false);
  error = signal('');
  message = signal('');
  private perfilGuardado = this.perfilSnapshot();
  cambiosPendientes=signal(false);
  private perfilSnapshot(){return JSON.stringify([this.nombre,this.avatar,this.temaElegido,this.carrera,this.plan]);}
  constructor(){this.vivo.watch(['perfil','cuentas'],async()=>{
    if(this.busy()||this.perfilSnapshot()!==this.perfilGuardado){this.cambiosPendientes.set(true);return;}
    const user=await this.api.request<import('../core/api').User>('/auth/me');
    if(this.busy()||this.perfilSnapshot()!==this.perfilGuardado){this.cambiosPendientes.set(true);return;}
    this.nombre=user.nombre;this.avatar=user.avatar??'turquesa';this.temaElegido=user.tema??'sistema';this.carrera=user.id_carrera??null;this.plan=user.id_plan??null;
    this.perfilGuardado=this.perfilSnapshot();this.cambiosPendientes.set(false);
  },inject(DestroyRef));}
  readonly avatars = ['turquesa', 'violeta'] as const;

  private async run(operation: () => Promise<void>) {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.message.set('');
    try {
      await operation();
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.busy.set(false);
    }
  }

  saveProfile() {
    return this.run(async () => {
      if (!this.nombre.trim()) throw new Error('Ingresa tu nombre visible.');
      await this.api.updateProfile({
        nombre: this.nombre.trim(),
        avatar: this.avatar,
        tema: this.temaElegido,
        id_carrera: this.carrera,
        id_plan: this.plan,
      });
      this.perfilGuardado=this.perfilSnapshot();this.cambiosPendientes.set(false);
      this.message.set('Cambios guardados.');
    });
  }

  savePhoto(input: HTMLInputElement) {
    return this.run(async () => {
      const file = input.files?.[0];
      if (!file) throw new Error('Selecciona una foto.');
      if (file.size > 2 * 1024 * 1024) throw new Error('La foto debe pesar hasta 2 MB.');
      await this.api.uploadPhoto(file);
      input.value = '';
      this.message.set('Foto guardada.');
    });
  }

  removePhoto() {
    return this.run(async () => {
      await this.api.removePhoto();
      this.message.set('Foto eliminada. Se muestra tu avatar.');
    });
  }

  savePassword() {
    return this.run(async () => {
      if (!this.actual) throw new Error('Ingresa la contraseña actual.');
      if (this.nueva.length < 8 || this.nueva !== this.confirmar)
        throw new Error('Usa al menos 8 caracteres y confirma la nueva contraseña.');
      await this.api.changePassword(this.actual, this.nueva);
      this.actual = this.nueva = this.confirmar = '';
      this.message.set('Contraseña actualizada.');
    });
  }
}
