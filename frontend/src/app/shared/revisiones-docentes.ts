import { Actualizaciones } from '../core/actualizaciones';
import {Component,effect,inject,input,signal, DestroyRef } from '@angular/core';
import {DatePipe} from '@angular/common';
import {Api} from '../core/api';
export interface RevisionDocente {id:string;ejemplar_id:number;version:number;vigente:boolean;docente:string;resultado:string;observaciones:string;fecha:string}
@Component({selector:'app-revisiones-docentes',imports:[DatePipe],template:`
  <h3>Revisiones docentes</h3>
  @if(loading()){<p role="status">Cargando revisiones…</p>}
  @if(error()){<p class="notice error" role="alert">{{error()}}</p><button class="secondary" (click)="load()">Reintentar</button>}
  @for(r of items();track r.id){<article class="panel">
    <strong>{{r.resultado}} · versión {{r.version}}</strong>
    <p class="muted">{{r.docente}} · {{r.fecha | date:'dd/MM/yyyy HH:mm':'-0600'}}
    </p>
    @if(!r.vigente){<p class="notice">Corresponde a un ejemplar anterior.</p>}
    <p style="white-space:pre-wrap;overflow-wrap:anywhere">{{r.observaciones}}</p>
  </article>}
  @if(!loading()&&!error()&&!items().length){<p class="muted">Todavía no hay revisiones docentes registradas.</p>}
`})
export class RevisionesDocentes {
 vivo=inject(Actualizaciones);
  path=input.required<string>();private api=inject(Api);items=signal<RevisionDocente[]>([]);loading=signal(false);error=signal('');
  private sequence=0;
  constructor(){this.vivo.watch(['revisiones','biblioteca','docente'],()=>this.load(true),inject(DestroyRef));effect(()=>{this.path();void this.load();});}
  async load(background=false){const n=++this.sequence;this.loading.set(true);this.error.set('');if(!background)this.items.set([]);
    try{const rows=await this.api.request<RevisionDocente[]>(this.path());if(n===this.sequence)this.items.set(rows);}
    catch(e:any){if(n===this.sequence)this.error.set(e.message);}
    finally{if(n===this.sequence)this.loading.set(false);}}
}
