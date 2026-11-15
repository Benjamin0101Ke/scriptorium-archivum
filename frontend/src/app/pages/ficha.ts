import {Component,DestroyRef,inject,signal} from '@angular/core';
import {ActivatedRoute,RouterLink} from '@angular/router';import {Api,Trabajo} from '../core/api';
import {DomSanitizer,SafeResourceUrl} from '@angular/platform-browser';
@Component({selector:'app-ficha',imports:[RouterLink],templateUrl:'./ficha.html'})
export class Ficha {
 api=inject(Api);route=inject(ActivatedRoute);t=signal<Trabajo|null>(null);error=signal('');message=signal('');
 private sanitizer=inject(DomSanitizer);
 private objectUrl:string|null=null;
 preview=signal<SafeResourceUrl|null>(null);
 pdfBusy=signal(false);
 constructor(){
  inject(DestroyRef).onDestroy(()=>this.closePreview());
  this.route.paramMap.subscribe(async params=>{
   this.closePreview();
   this.error.set('');
   this.message.set('');
   this.t.set(null);
   const id=params.get('id');
   try{
    const work=await this.api.request<Trabajo>('/trabajos/'+id);
    if(this.route.snapshot.paramMap.get('id')===id)this.t.set(work);
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
   const pdf=await this.api.request<Blob>('/trabajos/'+work.id+'/pdf?operacion='+operation);
   if(this.t()?.id!==work.id)return;
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
 async favorite(){try{await this.api.request('/favoritos/'+this.t()!.id,'PUT');this.message.set('Trabajo guardado en tu cuenta.');}catch(e:any){this.message.set(e.message);}}
 async share(){try{await navigator.clipboard.writeText(location.href);this.message.set('Enlace copiado.');}catch{this.message.set('Copiá el enlace de la barra de direcciones.');}}
}
