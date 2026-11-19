import {Component,inject,signal} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {RouterLink} from '@angular/router';
import {Api,Trabajo} from '../core/api';
@Component({selector:'app-catalogo',imports:[FormsModule,RouterLink],templateUrl:'./catalogo.html'})
export class Catalogo {
 api=inject(Api);items=signal<Trabajo[]>([]);all=signal<Trabajo[]>([]);loading=signal(true);error=signal('');
 q='';autor='';anio='';carrera='';area='';tipo='';orden='coincidencia';
 options(field:keyof Trabajo){return [...new Set(this.all().flatMap(t=>field==='autores'?t.autores:[String(t[field])]))].sort();}
 constructor(){this.load(true);}
 async load(initial=false){this.loading.set(true);this.error.set('');try{
 const query=new URLSearchParams({q:this.q,autor:this.autor,anio:this.anio,carrera:this.carrera,area:this.area,tipo:this.tipo,orden:this.orden});
 const data=await this.api.request<Trabajo[]>('/trabajos?'+query);this.items.set(data);if(initial)this.all.set(data);
 }catch(e:any){this.error.set(e.message);}finally{this.loading.set(false);}}
 reset(){this.q=this.autor=this.anio=this.carrera=this.area=this.tipo='';this.orden='coincidencia';this.load();}
}
