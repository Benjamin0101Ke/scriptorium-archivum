import { Actualizaciones } from './core/actualizaciones';
import { Component, DestroyRef, effect, HostListener, inject, signal, viewChild, ElementRef } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Api, puedeEnviar } from './core/api';
import { espaciosPorRol } from './core/roles';
import { PreferenciaTema } from './core/tema';
import { Avatar } from './shared/avatar';
import { AbrirConEfecto } from './core/transiciones';
import { TransicionVisual } from './shared/transicion-visual';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Avatar, AbrirConEfecto, TransicionVisual],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
 vivo=inject(Actualizaciones);
  api = inject(Api);
  puedeEnviar = puedeEnviar;
  espaciosPorRol = espaciosPorRol;
  private router = inject(Router);
  enSuperadmin(){return this.router.url==='/superadmin';}
  enRuta(ruta:string){return this.router.url.split(/[?#]/)[0]===ruta;}
  tema = inject(PreferenciaTema);
  private menu = viewChild<ElementRef<HTMLDetailsElement>>('accountMenu');
  error = signal('');
  cambiandoTema = signal(false);
  private sesionPresencia = crypto.randomUUID();

  constructor() {
    this.vivo.watch(['perfil','cuentas'],async()=>{
      const anterior=this.api.user();if(!anterior)return;
      try{const actual=await this.api.request<import('./core/api').User>('/auth/me');this.api.user.set(actual);
        if(actual.rol!==anterior.rol)await this.router.navigateByUrl('/'+espaciosPorRol[actual.rol].ruta);
      }catch(e:any){if(e.status===401)await this.router.navigateByUrl('/ingresar');}
    },inject(DestroyRef));
    effect(() => { if (this.api.user()) void this.marcarPresencia(); });
    const reloj = setInterval(() => void this.marcarPresencia(), 30000);
    inject(DestroyRef).onDestroy(() => clearInterval(reloj));
  }

  @HostListener('document:visibilitychange')
  async marcarPresencia() {
    if (this.api.demo || !this.api.user() || document.hidden) return;
    try { await this.api.request('/presencia', 'POST', { sesion: this.sesionPresencia }); }
    catch { /* El próximo intervalo vuelve a comprobar la conexión. */ }
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: MouseEvent) {
    const menu = this.menu()?.nativeElement;
    if (menu?.open && event.target instanceof Node && !menu.contains(event.target))
      menu.open = false;
  }

  closeMenu(restoreFocus = false) {
    const menu = this.menu()?.nativeElement;
    if (!menu?.open) return;
    menu.open = false;
    if (restoreFocus) menu.querySelector('summary')?.focus();
  }

  async toggleTheme() {
    if (this.cambiandoTema()) return;
    this.cambiandoTema.set(true);
    this.error.set('');
    try {
      await this.tema.set(this.tema.oscuro() ? 'claro' : 'oscuro');
    } catch (e: any) {
      this.error.set(e.message);
    } finally {
      this.cambiandoTema.set(false);
    }
  }

  async logout() {
    this.closeMenu();
    this.error.set('');
    try {
      await this.api.logout();
      await this.router.navigateByUrl('/');
    } catch (e: any) {
      this.error.set(e.message);
    }
  }
}
