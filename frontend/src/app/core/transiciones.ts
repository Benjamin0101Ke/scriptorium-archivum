import {Directive,ElementRef,HostBinding,HostListener,Injectable,inject,input,signal} from '@angular/core';
import {Router} from '@angular/router';
export type TipoApertura='archivo'|'portal'|'catalogo'|'favoritos'|'envios';
export interface Escena {tipo:TipoApertura|'envio';titulo:string;simbolo:string;cerrando:boolean}
@Injectable({providedIn:'root'})
export class Transiciones {
  escena=signal<Escena|null>(null);
  private router=inject(Router);
  private entrar?:()=>void;
  private cancelarEnfoque?:()=>void;
  private enfocarOmitir(){requestAnimationFrame(()=>document.querySelector<HTMLButtonElement>('app-transicion-visual .omitir')?.focus({preventScroll:true}));}
  async abrir(destino:string,titulo:string,tipo:TipoApertura='archivo',simbolo='superadmin'){
    if(this.escena())return;
    this.cancelarEnfoque?.();
    if(matchMedia('(prefers-reduced-motion: reduce)').matches){await this.router.navigateByUrl(destino);this.enfocarDestino();return;}
    const origen=this.router.url;
    this.escena.set({tipo,titulo,simbolo,cerrando:false});
    this.enfocarOmitir();
    await new Promise<void>(resolve=>{let timer=setTimeout(resolve,tipo==='portal'?1450:1300);this.entrar=()=>{clearTimeout(timer);resolve();};});
    this.entrar=undefined;
    try{if(this.router.url===origen)await this.router.navigateByUrl(destino);}
    finally{await this.cerrar();}
    this.enfocarDestino();
  }
  confirmar(titulo:string){
    if(this.escena()||matchMedia('(prefers-reduced-motion: reduce)').matches)return;
    this.escena.set({tipo:'envio',titulo,simbolo:'biblioteca',cerrando:false});
    const anterior=document.activeElement as HTMLElement|null;
    this.enfocarOmitir();
    const terminar=async()=>{this.entrar=undefined;await this.cerrar();if(anterior?.isConnected)anterior.focus({preventScroll:true});};
    let timer=setTimeout(()=>void terminar(),1700);
    this.entrar=()=>{clearTimeout(timer);void terminar();};
  }
  omitir(){this.entrar?.();}
  private enfocarDestino(){
    const main=document.querySelector<HTMLElement>('main');if(!main)return;
    const ruta=this.router.url;
    const limpiar=()=>{observer.disconnect();clearTimeout(timer);this.cancelarEnfoque=undefined;};
    const enfocar=()=>{if(this.router.url!==ruta){limpiar();return;}const heading=main.querySelector<HTMLElement>('h1');if(heading){heading.tabIndex=-1;heading.focus({preventScroll:true});limpiar();}};
    const observer=new MutationObserver(enfocar),timer=setTimeout(limpiar,15000);
    this.cancelarEnfoque=limpiar;observer.observe(main,{childList:true,subtree:true});
    main.tabIndex=-1;main.focus({preventScroll:true});enfocar();
  }
  private async cerrar(){this.escena.update(s=>s?{...s,cerrando:true}:null);await new Promise(r=>setTimeout(r,350));this.escena.set(null);}
}
@Directive({selector:'a[abrirConEfecto]'})
export class AbrirConEfecto {
  abrirConEfecto=input.required<string>();
  efectoTitulo=input('Abriendo trabajo');
  efectoTipo=input<TipoApertura>('archivo');
  efectoSimbolo=input('superadmin');
  private transiciones=inject(Transiciones);
  private router=inject(Router);
  private el=inject(ElementRef<HTMLAnchorElement>);
  @HostBinding('attr.href')get href(){return this.router.serializeUrl(this.router.parseUrl(this.abrirConEfecto()));}
  @HostListener('click',['$event'])abrir(event:MouseEvent){
    if(event.button!==0||event.ctrlKey||event.metaKey||event.shiftKey||event.altKey||this.el.nativeElement.target==='_blank')return;
    event.preventDefault();
    void this.transiciones.abrir(this.abrirConEfecto(),this.efectoTitulo(),this.efectoTipo(),this.efectoSimbolo());
  }
}
