import {Injectable,signal} from '@angular/core';
import {settings} from './settings';
export type Avatar = 'turquesa' | 'violeta';
export type Tema = 'sistema' | 'claro' | 'oscuro';
export type Rol = 'Biblioteca' | 'Lector' | 'Estudiante' | 'Docente revisor' | 'Administrador' | 'Superadministrador';
export const puedeEnviar = (rol?: Rol) => rol === 'Lector' || rol === 'Estudiante' || rol === 'Superadministrador';
export interface User {id:string;nombre:string;correo:string;rol:Rol;avatar?:Avatar;tema?:Tema;foto?:string|null;id_carrera?:number|null;id_plan?:number|null}
export interface Trabajo {id:string;titulo:string;autores:string[];anio:number;carrera:string;area:string;areas?:string[];tipo:string;resumen:string;lectura:boolean;descarga:boolean;estado?:string;estrellas?:number;enlaces?:{id:string;url:string;tipo:string;descripcion:string}[]}
@Injectable({providedIn:'root'})
export class Api {
 demo=settings.demo;
 readonly cargaProgreso=signal<{nombre:string;porcentaje:number;procesando:boolean}|null>(null);
 private cargaAbortador?:AbortController;
 user=signal<User|null>(null);ready=this.restore();
 async pdf(path:string):Promise<Blob>{
 const pdf=await this.request<Blob>(path,'GET',undefined,'pdf');
 if(!(pdf instanceof Blob)||await pdf.slice(0,5).text()!=='%PDF-')
  throw new Error('La API no devolvió un PDF válido.');
 return pdf.type==='application/pdf'?pdf:new Blob([pdf],{type:'application/pdf'});
 }
 async pdfUrl(path:string):Promise<string>{
  if(this.demo)return URL.createObjectURL(await this.pdf(path));
  const url=settings.apiUrl+path;
  const r=await fetch(url,{credentials:'same-origin',headers:{Range:'bytes=0-4'}});
  if(!r.ok){const d=await r.json().catch(()=>({error:'No se pudo abrir el PDF.'}));throw new Error(d.error);}
  if(!r.headers.get('Content-Type')?.startsWith('application/pdf')){await r.body?.cancel();throw new Error('El archivo no es un PDF válido.');}
  if(r.status===206){if(await r.text()!=='%PDF-')throw new Error('El archivo no es un PDF válido.');}
  else await r.body?.cancel();
  return url;
 }
 async request<T=any>(path:string,method='GET',body?:unknown,format:'json'|'pdf'='json'):Promise<T> {
 if(this.demo)return (await import('./demo')).demoRequest(path,method,body);
 if(body instanceof FormData&&path!=='/auth/foto'&&method==='POST'){
  const archivo=body.get('archivo');
  if(archivo instanceof File){
   if(archivo.size>6*1024*1024*1024)throw new Error('El archivo supera el máximo de 6 GB.');
   if(archivo.size>8*1024*1024)return this.enviarArchivo<T>(path,body,archivo);
  }
 }
 const form=body instanceof FormData;let response:Response;
 const headers=new Headers({Accept:format==='pdf'?'application/pdf':'application/json'});
 if(body&&!form)headers.set('Content-Type','application/json');
 try{response=await fetch(settings.apiUrl+path,{method,credentials:'same-origin',headers,body:body?(form?body:JSON.stringify(body)):undefined});}
 catch{throw new Error('No se pudo conectar. Verifica que la API esté encendida.');}
 if(response.ok&&format==='pdf')return (await response.blob()) as T;
 const data=await response.json().catch(()=>({error:'Respuesta inesperada. Revisa la conexión con la API.'}));
 if(!response.ok){
  if(response.status===401&&path!=='/auth/login'&&this.user()){
   this.user.set(null);
   throw Object.assign(new Error('La sesión venció. Vuelve a iniciar sesión.'),{status:401,details:data});
  }
  throw Object.assign(new Error(data.error||'No se pudo completar la operación.'),{status:response.status,details:data});
 }
 return data;
 }
 cancelarCarga(){this.cargaAbortador?.abort();}
 private async enviarArchivo<T>(path:string,body:FormData,archivo:File):Promise<T>{
  if(this.cargaProgreso())throw new Error('Espera a que termine la carga actual.');
  const abortador=new AbortController();this.cargaAbortador=abortador;
  this.cargaProgreso.set({nombre:archivo.name,porcentaje:0,procesando:false});
  let id:string|undefined;
  try{
   const carga=await this.request<{id:string;parte_bytes:number}>('/cargas','POST',{nombre:archivo.name,tamano_bytes:archivo.size});
   id=carga.id;
   for(let inicio=0,parte=0;inicio<archivo.size;inicio+=carga.parte_bytes,parte++){
    const fin=Math.min(inicio+carga.parte_bytes,archivo.size);
    for(let intento=0;;intento++){
     try{
      const r=await fetch(settings.apiUrl+'/cargas/'+id+'/partes/'+parte,{method:'PUT',credentials:'same-origin',headers:{'Content-Type':'application/octet-stream'},body:archivo.slice(inicio,fin),signal:abortador.signal});
      if(!r.ok){
       const datos=await r.json().catch(()=>({error:'No se pudo guardar esta parte del archivo.'}));
       if(r.status===401)this.user.set(null);
       throw Object.assign(new Error(datos.error),{status:r.status});
      }
      break;
     }catch(e:any){
      if(abortador.signal.aborted||intento>=2||(e.status&&e.status<500&&![408,429].includes(e.status)))throw e;
      await new Promise(resolve=>setTimeout(resolve,1000*(intento+1)));
     }
    }
    this.cargaProgreso.set({nombre:archivo.name,porcentaje:Math.floor(fin*100/archivo.size),procesando:fin===archivo.size});
   }
   if(abortador.signal.aborted)throw new Error('Carga cancelada.');
   const datos=new FormData();
   body.forEach((value,key)=>{if(key!=='archivo')datos.append(key,value);});
   datos.set('carga_id',id);
   return await this.request<T>(path,'POST',datos);
  }catch(e:any){
   if(abortador.signal.aborted)throw new Error('Carga cancelada.');
   throw e;
  }finally{
   if(id)void fetch(settings.apiUrl+'/cargas/'+id,{method:'DELETE',credentials:'same-origin',signal:AbortSignal.timeout(10000)}).catch(()=>undefined);
   this.cargaAbortador=undefined;this.cargaProgreso.set(null);
  }
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
