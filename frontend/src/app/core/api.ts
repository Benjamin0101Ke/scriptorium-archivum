import {Injectable,signal} from '@angular/core';
import {settings} from './settings';
export type Avatar = 'turquesa' | 'violeta';
export type Tema = 'sistema' | 'claro' | 'oscuro';
export type Rol = 'Biblioteca' | 'Lector' | 'Estudiante' | 'Docente revisor' | 'Administrador';
export const puedeEnviar = (rol?: Rol) => rol === 'Lector' || rol === 'Estudiante';
export interface User {id:string;nombre:string;correo:string;rol:Rol;avatar?:Avatar;tema?:Tema;foto?:string|null;id_carrera?:number|null;id_plan?:number|null}
export interface Trabajo {id:string;titulo:string;autores:string[];anio:number;carrera:string;area:string;areas?:string[];tipo:string;resumen:string;lectura:boolean;descarga:boolean;estado?:string;valoracion_media?:number;valoraciones_total?:number;enlaces?:{id:string;url:string;tipo:string;descripcion:string}[]}
@Injectable({providedIn:'root'})
export class Api {
 demo=settings.demo;
 user=signal<User|null>(null);ready=this.restore();
 async pdf(path:string):Promise<Blob>{
 const pdf=await this.request<Blob>(path,'GET',undefined,'pdf');
 if(!(pdf instanceof Blob)||await pdf.slice(0,5).text()!=='%PDF-')
  throw new Error('La API no devolvió un PDF válido.');
 return pdf.type==='application/pdf'?pdf:new Blob([pdf],{type:'application/pdf'});
 }
 async request<T=any>(path:string,method='GET',body?:unknown,format:'json'|'pdf'='json'):Promise<T> {
 if(this.demo)return (await import('./demo')).demoRequest(path,method,body);
 const form=body instanceof FormData;let response:Response;
 const headers=new Headers({Accept:format==='pdf'?'application/pdf':'application/json'});
 if(body&&!form)headers.set('Content-Type','application/json');
 try{response=await fetch(settings.apiUrl+path,{method,credentials:'same-origin',headers,body:body?(form?body:JSON.stringify(body)):undefined});}
 catch{throw new Error('No se pudo conectar. Verificá que la API esté encendida.');}
 if(response.ok&&format==='pdf')return (await response.blob()) as T;
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
 async register(nombres:string,apellidos:string,correo:string,password:string,id_carrera?:number|null,id_plan?:number|null){
  this.user.set(await this.request<User>('/auth/registro','POST',{nombres,apellidos,correo,password,id_carrera,id_plan}));
 }
 async updateProfile(profile:{nombre?:string;avatar?:Avatar;tema?:Tema;id_carrera?:number|null;id_plan?:number|null}){
  this.user.set(await this.request<User>('/auth/perfil','PUT',profile));
 }
 async changePassword(actual:string,nueva:string){
  await this.request('/auth/contrasena','PUT',{actual,nueva});
 }
 async uploadPhoto(file:File){
  const body=new FormData();
  body.set('archivo',file);
  this.user.set(await this.request<User>('/auth/foto','POST',body));
 }
 async removePhoto(){this.user.set(await this.request<User>('/auth/foto','DELETE'));}
 async logout(){await this.request('/auth/logout','POST');this.user.set(null);}
}
