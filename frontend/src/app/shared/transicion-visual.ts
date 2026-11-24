import {Component,HostListener,inject} from '@angular/core';
import {Transiciones} from '../core/transiciones';
import {IconoRol} from './icono-rol';
import {EscenaColeccion} from './escena-coleccion';
@Component({selector:'app-transicion-visual',imports:[IconoRol,EscenaColeccion],template:`
@if(efecto.escena();as escena){
 <section class="escena" [class.salida]="escena.cerrando" [class.portal]="escena.tipo==='portal'" [class.envio]="escena.tipo==='envio'" [attr.data-escena]="escena.tipo" aria-label="Transición de navegación">
  <button type="button" class="omitir" (click)="efecto.omitir()">{{escena.tipo==='envio'?'Continuar':'Entrar ahora'}} <span aria-hidden="true">↗</span></button>
  <div class="ambiente" aria-hidden="true"><i></i><i></i><i></i></div>
  <div class="escena-centro">
   @if(escena.tipo==='archivo'){
    <div class="carpeta-espacio" aria-hidden="true"><div class="carpeta">
     <div class="carpeta-fondo"></div>
     <div class="papel hoja-uno"><b>ARCHIVUM</b><i></i><i></i><i></i><i></i><i></i></div>
     <div class="papel hoja-dos"><b>CONOCIMIENTO</b><i></i><i></i><i></i><i></i><i></i></div>
     <div class="papel hoja-tres"><b>DESCUBRE</b><i></i><i></i><i></i><i></i><i></i></div>
     <div class="carpeta-frente"><span>SA /</span><strong>ARCHIVUM</strong><small>Una nueva historia por descubrir</small></div>
    </div></div>
   }@else if(escena.tipo==='portal'){
    <div class="boveda" [attr.data-simbolo]="escena.simbolo" aria-hidden="true"><div class="anillo exterior"></div><div class="anillo interior"></div><div class="orbita"></div><div class="nucleo"><app-icono-rol [nombre]="escena.simbolo" /></div><div class="puerta izquierda"></div><div class="puerta derecha"></div><span class="destello"></span></div>
   }@else if(escena.tipo==='envio'){
    <div class="entrega" aria-hidden="true"><div class="trayectoria"></div><div class="documento"><span>ARCHIVUM</span><i></i><i></i><i></i></div><div class="bandeja"><app-icono-rol nombre="biblioteca"/></div><div class="sello">✓</div></div>
   }@else{<app-escena-coleccion [tipo]="escena.tipo" />}
   <div class="leyenda" role="status" aria-live="polite"><span>{{escena.tipo==='portal'?'SCRIPTORIUM · ESPACIOS':escena.tipo==='envio'?'ENVÍO COMPLETADO':'SCRIPTORIUM · ARCHIVUM'}}</span><h2>{{escena.titulo}}</h2><p>{{escena.tipo==='envio'?'Tu trabajo ya está en revisión en Biblioteca.':escena.tipo==='portal'?'Tu próximo espacio, a un paso.':escena.tipo==='favoritos'?'Las ideas que guardaste, en un solo lugar.':escena.tipo==='envios'?'Tus trabajos y el recorrido de cada entrega.':escena.tipo==='catalogo'?'Descubre lo que nuestra comunidad crea.':'Abriendo las páginas del conocimiento.'}}</p></div>
  </div>
 </section>
}`,styleUrl:'./transicion-visual.css'})
export class TransicionVisual {
 efecto=inject(Transiciones);
 @HostListener('document:keydown.escape')omitir(){this.efecto.omitir();}
 @HostListener('document:keydown',['$event'])teclado(event:KeyboardEvent){if(this.efecto.escena()&&event.key==='Tab'){event.preventDefault();document.querySelector<HTMLButtonElement>('app-transicion-visual .omitir')?.focus();}}
}
