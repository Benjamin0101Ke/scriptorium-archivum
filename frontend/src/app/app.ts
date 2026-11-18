import { Component, HostListener, inject, signal, viewChild, ElementRef } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Api, puedeEnviar } from './core/api';
import { espaciosPorRol } from './core/roles';
import { PreferenciaTema } from './core/tema';
import { Avatar } from './shared/avatar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Avatar],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  api = inject(Api);
  puedeEnviar = puedeEnviar;
  espaciosPorRol = espaciosPorRol;
  private router = inject(Router);
  tema = inject(PreferenciaTema);
  private menu = viewChild<ElementRef<HTMLDetailsElement>>('accountMenu');
  error = signal('');
  cambiandoTema = signal(false);

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
