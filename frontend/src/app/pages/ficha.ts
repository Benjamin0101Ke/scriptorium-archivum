import {Component,DestroyRef,inject,signal} from '@angular/core';
import {ActivatedRoute,RouterLink} from '@angular/router';import {Api,Trabajo} from '../core/api';
import {DomSanitizer,SafeResourceUrl} from '@angular/platform-browser';
import {ArchivosTrabajo} from '../shared/archivos-trabajo';
@Component({selector:'app-ficha',imports:[RouterLink,ArchivosTrabajo],templateUrl:'./ficha.html'})
export class Ficha {
 api=inject(Api);route=inject(ActivatedRoute);t=signal<Trabajo|null>(null);error=signal('');message=signal('');
 private sanitizer=inject(DomSanitizer);
 private destroyRef=inject(DestroyRef);
 private objectUrl:string|null=null;
 preview=signal<SafeResourceUrl|null>(null);
 pdfBusy=signal(false);
 favorito=signal(false);
 favoritoBusy=signal(false);
 constructor(){
  this.destroyRef.onDestroy(()=>this.closePreview());
  this.route.paramMap.subscribe(async params=>{
   this.closePreview();
   this.error.set('');
   this.message.set('');
   this.t.set(null);
   const id=params.get('id');
   try{
    const work=await this.api.request<Trabajo>('/trabajos/'+id);
    if(this.route.snapshot.paramMap.get('id')===id){this.t.set(work);this.cargarFavorito(id!);}
   }catch(e:any){if(this.route.snapshot.paramMap.get('id')===id)this.error.set(e.message);}
  });
 }
 closePreview(){
  if(this.objectUrl)URL.revokeObjectURL(this.objectUrl);
  this.objectUrl=null;
  this.preview.set(null);
 }
 async localPdf(operation:'lectura'|'descarga'){
  const work=this.t();
  if(!work||this.pdfBusy())return;
  this.message.set('');
  this.pdfBusy.set(true);
  try{
   const pdf=await this.api.pdf('/trabajos/'+work.id+'/pdf?operacion='+operation);
   if(this.destroyRef.destroyed||this.t()?.id!==work.id)return;
   const url=URL.createObjectURL(pdf);
   if(operation==='lectura'){
    this.closePreview();
    this.objectUrl=url;
    this.preview.set(this.sanitizer.bypassSecurityTrustResourceUrl(url));
   }else{
    const link=document.createElement('a');
    link.href=url;
    link.download=work.titulo+'.pdf';
    link.click();
    setTimeout(()=>URL.revokeObjectURL(url),30_000);
   }
  }catch(e:any){this.message.set(e.message);}finally{this.pdfBusy.set(false);}
 }
 private async cargarFavorito(id:string){
   if(!this.api.user()){this.favorito.set(false);return;}
   try{
     const lista=await this.api.request<Trabajo[]>('/favoritos');
     if(this.t()?.id===id)this.favorito.set(lista.some((item)=>item.id===id));
   }catch{/* Sin favoritos o sin sesión: se muestra la opción de guardar. */}
 }
 async toggleFavorite(){
   const work=this.t();
   if(!work||this.favoritoBusy())return;
   this.favoritoBusy.set(true);
   this.message.set('');
   try{
     if(this.favorito()){
       await this.api.request('/favoritos/'+work.id,'DELETE');
       this.favorito.set(false);
       this.t.update((actual)=>actual?{...actual,estrellas:Math.max(0,(actual.estrellas??0)-1)}:actual);
       this.message.set('Quitaste esta ficha de tus favoritos.');
     }else{
       await this.api.request('/favoritos/'+work.id,'PUT');
       this.favorito.set(true);
       this.t.update((actual)=>actual?{...actual,estrellas:(actual.estrellas??0)+1}:actual);
       this.message.set('Trabajo guardado en tu cuenta.');
     }
   }catch(e:any){this.message.set(e.message);}
   finally{this.favoritoBusy.set(false);}
 }
 async share(){try{await navigator.clipboard.writeText(location.href);this.message.set('Enlace copiado.');}catch{this.message.set('Copiá el enlace de la barra de direcciones.');}}
}
