import { Actualizaciones } from '../core/actualizaciones';
import {Component,DestroyRef,HostListener,inject,signal} from '@angular/core';
import {Api} from '../core/api';
interface Asignacion {estado:'Asignado'|'Sin asignación'|'No disponible';docente:{nombre:string;correo:string}|null}
@Component({selector:'app-mi-docente',template:`
  @if(api.user()?.rol==='Estudiante'){
    <section class="panel" aria-label="Mi docente asignado">
      <h2>Mi docente asignado</h2>
      @if(loading()&&!asignacion()){<p role="status">Consultando tu asignación…</p>}
      @if(error()){<p class="notice error" role="alert">{{error()}}</p>}
      @if(asignacion();as a){
        <div class="notice" role="status" aria-live="polite">
          @if(a.docente;as d){<p>Tu docente asignado es <strong>{{d.nombre}}</strong>.</p>
            <p>Puedes comunicarte con él o ella en <a [href]="'mailto:'+d.correo">{{d.correo}}</a>.</p>}
          @else if(a.estado==='No disponible'){<p>Tu docente asignado no está disponible actualmente. Comunícate con Administración para revisar tu asignación.</p>}
          @else{<p>Todavía no tienes un docente asignado. Administración puede asignártelo desde la gestión de cuentas.</p>}
        </div>
      }
      <button type="button" class="secondary small" (click)="load()" [disabled]="loading()">Actualizar docente</button>
    </section>
  }
`,styles:`.notice{overflow-wrap:anywhere}.notice p{margin-block:.4rem}button{margin-top:12px}`})
export class MiDocente {
 vivo=inject(Actualizaciones);
  api=inject(Api);private destroyRef=inject(DestroyRef);asignacion=signal<Asignacion|null>(null);loading=signal(false);error=signal('');
  constructor(){
    this.vivo.watch(['asignacion','cuentas'],()=>this.load(),this.destroyRef);
    void this.api.ready.then(()=>this.load());
    const timer=setInterval(()=>{if(!document.hidden)void this.load();},30000);
    this.destroyRef.onDestroy(()=>clearInterval(timer));
  }
  @HostListener('window:focus') actualizar(){void this.load();}
  async load(){
    if(this.destroyRef.destroyed||this.loading()||this.api.user()?.rol!=='Estudiante'||this.api.demo)return;
    this.loading.set(true);this.error.set('');
    try{const a=await this.api.request<Asignacion>('/mi-docente');if(!this.destroyRef.destroyed)this.asignacion.set(a);}
    catch(e:any){if(!this.destroyRef.destroyed){this.asignacion.set(null);this.error.set(e.message);}}
    finally{if(!this.destroyRef.destroyed)this.loading.set(false);}
  }
}
