import {Component,inject,signal} from '@angular/core';import {RouterLink} from '@angular/router';import {Api,Trabajo} from '../core/api';
@Component({selector:'app-favoritos',imports:[RouterLink],templateUrl:'./favoritos.html'})
export class Favoritos {api=inject(Api);items=signal<Trabajo[]>([]);error=signal('');loading=signal(true);constructor(){this.load();}
async load(){try{this.items.set(await this.api.request('/favoritos'));}catch(e:any){this.error.set(e.message);}finally{this.loading.set(false);}}
async remove(id:string){try{await this.api.request('/favoritos/'+id,'DELETE');await this.load();}catch(e:any){this.error.set(e.message);}}}
