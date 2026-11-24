import { Actualizaciones } from '../core/actualizaciones';
import { AbrirConEfecto } from '../core/transiciones';
import {Component,inject,signal, DestroyRef } from '@angular/core';import {RouterLink} from '@angular/router';import {Api,Trabajo} from '../core/api';
@Component({selector:'app-favoritos',imports:[RouterLink,AbrirConEfecto],templateUrl:'./favoritos.html'})
export class Favoritos {
 vivo=inject(Actualizaciones);api=inject(Api);items=signal<Trabajo[]>([]);error=signal('');loading=signal(true);constructor(){this.vivo.watch(['favoritos','catalogo'],()=>this.load(),inject(DestroyRef));this.load();}
async load(){try{this.items.set(await this.api.request('/favoritos'));}catch(e:any){this.error.set(e.message);}finally{this.loading.set(false);}}
async remove(id:string){try{await this.api.request('/favoritos/'+id,'DELETE');await this.load();}catch(e:any){this.error.set(e.message);}}}
