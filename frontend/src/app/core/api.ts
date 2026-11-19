import {Injectable,signal} from '@angular/core';
import {settings} from './settings';
export interface User {id:string;nombre:string;correo:string;rol:'Biblioteca'|'Lector'}
export interface Trabajo {id:string;titulo:string;autores:string[];anio:number;carrera:string;area:string;tipo:string;resumen:string;lectura:boolean;descarga:boolean;estado?:string;enlaces?:{id:string;url:string;tipo:string;descripcion:string}[]}
@Injectable({providedIn:'root'})
export class Api {
 demo=settings.demo;
 user=signal<User|null>(null);ready=this.restore();
 async request<T=any>(path:string,method='GET',body?:unknown):Promise<T> {
 if(this.demo)return (await import('./demo')).demoRequest(path,method,body);
 const form=body instanceof FormData;let response:Response;
 try{response=await fetch(settings.apiUrl+path,{method,credentials:'same-origin',headers:body&&!form?{'Content-Type':'application/json'}:{},body:body?(form?body:JSON.stringify(body)):undefined});}
 catch{throw new Error('No se pudo conectar. Verificá que la API esté encendida.');}
 const data=await response.json().catch(()=>({error:'Respuesta inesperada. Revisá la conexión con la API.'}));
 if(!response.ok){
  if(response.status===401&&path!=='/auth/login'&&this.user()){
   this.user.set(null);
   throw Object.assign(new Error('La sesión venció. Volvé a iniciar sesión.'),{status:401,details:data});
  }
  throw Object.assign(new Error(data.error||'No se pudo completar la operación.'),{status:response.status,details:data});
 }
 return data;
 }
 async restore(){try{this.user.set(await this.request<User>('/auth/me'));}catch{this.user.set(null);}}
 async login(correo:string,password:string){this.user.set(await this.request<User>('/auth/login','POST',{correo,password}));}
 async registerLocal(nombre:string,correo:string,password:string){
  if(!this.demo)throw new Error('El registro local está disponible en la demostración.');
  this.user.set(await this.request<User>('/auth/registro','POST',{nombre,correo,password}));
 }
 async logout(){await this.request('/auth/logout','POST');this.user.set(null);}
}
