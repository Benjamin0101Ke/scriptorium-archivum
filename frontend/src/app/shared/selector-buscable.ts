import {Component,ElementRef,HostListener,computed,inject,input,model,signal,viewChild} from '@angular/core';
let secuencia=0;
@Component({selector:'app-selector-buscable',template:`
<span class="etiqueta" [id]="id+'-label'">{{etiqueta()}}</span>
<button #boton type="button" class="disparador" [attr.aria-labelledby]="id+'-label '+id+'-valor'" aria-haspopup="listbox" [attr.aria-expanded]="abierto()" [attr.aria-controls]="abierto()?id+'-lista':null" (click)="alternar()" (keydown.arrowdown)="abrir($event)" (keydown.arrowup)="abrir($event)">
 <span [id]="id+'-valor'">{{valor()||todas()}}</span><svg viewBox="0 0 16 16" aria-hidden="true"><path d="m4 6 4 4 4-4"/></svg>
</button>
@if(abierto()){
 <div class="menu">
  <div class="buscar"><input #busqueda type="search" [value]="consulta()" (input)="filtrar($event)" [attr.aria-label]="'Buscar en '+etiqueta()" role="combobox" aria-autocomplete="list" aria-expanded="true" [attr.aria-controls]="id+'-lista'" [attr.aria-activedescendant]="filtradas().length?id+'-opcion-'+activo():null" placeholder="Escribe para buscar…" (keydown)="teclado($event)" /></div>
  <div class="opciones" [id]="id+'-lista'" role="listbox" [attr.aria-label]="etiqueta()">
   @for(opcion of filtradas();track opcion;let i=$index){
    <button type="button" role="option" tabindex="-1" [id]="id+'-opcion-'+i" [attr.aria-selected]="valor()===opcion" [class.resaltada]="activo()===i" (pointermove)="activo.set(i)" (click)="elegir(opcion)"><span>{{opcion||todas()}}</span>@if(valor()===opcion){<b aria-hidden="true">✓</b>}</button>
   }@empty{<p role="status">No hay coincidencias.</p>}
  </div>
 </div>
}`,styles:`:host{position:relative;display:block;min-width:0}.etiqueta{display:block;font-size:13px;font-weight:700;margin-bottom:8px}.disparador{width:100%;min-height:46px;display:flex;justify-content:space-between;align-items:center;gap:10px;text-align:left;color:var(--text);background:var(--input-bg,var(--surface));border:1px solid var(--line);border-radius:12px;padding:10px 14px;font:inherit;font-size:13px;box-shadow:none}.disparador span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.disparador svg{width:16px;height:16px;flex-shrink:0;fill:none;stroke:var(--accent,#9f83ff);stroke-width:1.8}.menu{position:absolute;top:100%;left:0;z-index:40;width:100%;min-width:min(290px,calc(100vw - 80px));margin-top:7px;background:var(--surface);border:1px solid var(--line-strong,var(--line));border-radius:14px;box-shadow:0 18px 45px #0005;overflow:hidden}.buscar{padding:10px;border-bottom:1px solid var(--line)}.buscar input{width:100%;margin:0;height:40px;font-size:13px;padding:9px 12px}.opciones{max-height:220px;overflow-y:auto;overscroll-behavior:contain;scrollbar-width:thin}.opciones button{width:100%;height:44px;display:flex;align-items:center;justify-content:space-between;gap:10px;border:0;border-radius:0;background:transparent;color:var(--text);font:inherit;font-size:13px;font-weight:400;text-align:left;padding:0 13px;box-shadow:none}.opciones button span{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.opciones button.resaltada,.opciones button:hover{background:color-mix(in srgb,var(--accent,#9879ff) 15%,transparent)}.opciones button[aria-selected=true]{color:var(--accent,#9879ff);font-weight:700}.opciones p{padding:14px;margin:0;font-size:13px;color:var(--muted)}button:focus-visible{outline:2px solid var(--accent,#9879ff);outline-offset:2px}`})
export class SelectorBuscable {
 etiqueta=input.required<string>();opciones=input<string[]>([]);todas=input('Todas');valor=model('');
 abierto=signal(false);consulta=signal('');activo=signal(0);id='selector-buscable-'+(++secuencia);
 private host=inject(ElementRef<HTMLElement>);private busqueda=viewChild<ElementRef<HTMLInputElement>>('busqueda');private boton=viewChild<ElementRef<HTMLButtonElement>>('boton');
 private normalizar(texto:string){return texto.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLocaleLowerCase('es').trim();}
 filtradas=computed(()=>{const q=this.normalizar(this.consulta());return ['',...this.opciones()].filter(v=>!q||!!v&&this.normalizar(v).includes(q));});
 alternar(){if(this.abierto())this.cerrar();else this.abrir();}
 abrir(event?:Event){event?.preventDefault();this.consulta.set('');this.activo.set(0);this.abierto.set(true);requestAnimationFrame(()=>this.busqueda()?.nativeElement.focus());}
 filtrar(event:Event){this.consulta.set((event.target as HTMLInputElement).value);this.activo.set(0);}
 elegir(valor:string){this.valor.set(valor);this.cerrar(true);}
 cerrar(enfocar=false){this.abierto.set(false);if(enfocar)this.boton()?.nativeElement.focus();}
 teclado(event:KeyboardEvent){
  if(event.key==='Escape'){event.preventDefault();this.cerrar(true);}
  else if(event.key==='Tab')this.cerrar();
  else if(event.key==='ArrowDown'||event.key==='ArrowUp'){event.preventDefault();const n=this.filtradas().length;if(!n)return;this.activo.set((this.activo()+(event.key==='ArrowDown'?1:-1)+n)%n);document.getElementById(this.id+'-opcion-'+this.activo())?.scrollIntoView({block:'nearest'});}
  else if(event.key==='Enter'){event.preventDefault();const opcion=this.filtradas()[this.activo()];if(opcion!==undefined)this.elegir(opcion);}
 }
 @HostListener('document:pointerdown',['$event'])fuera(event:PointerEvent){if(!this.host.nativeElement.contains(event.target as Node))this.cerrar();}
}
