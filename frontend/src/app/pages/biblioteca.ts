import {Component,ElementRef,inject,signal,viewChild} from '@angular/core';import {FormsModule} from '@angular/forms';import {Api} from '../core/api';
import {createMetadataDraft,metadataFromDraft,validateMetadata} from '../core/metadata';
import {MetadataFields} from '../shared/metadata-fields';
import {LocalPdfPreview} from '../shared/local-pdf-preview';
const emptyControls=()=>({ejemplar_id:'',deposito_referencia:'',deposito_fecha:'',deposito_verificado:false,verificar:false,conformidad_referencia:'',ficha_publica:false,lectura:false,descarga:false,autorizacion:{referencia:'',otorgantes:'',fecha:new Date().toISOString().slice(0,10),vence:'',estado:'Vigente',ficha:false,lectura:false,descarga:false},decision:{resultado:'Favorable',motivo:''}});
@Component({selector:'app-biblioteca',imports:[FormsModule,MetadataFields,LocalPdfPreview],templateUrl:'./biblioteca.html'})
export class Biblioteca {
 api=inject(Api);items=signal<any[]>([]);current=signal<any>(null);checks=signal<any[]>([]);logs=signal<any[]>([]);report=signal<any[]>([]);links=signal<any[]>([]);
 private element=inject<ElementRef<HTMLElement>>(ElementRef);
 private metadataFields=viewChild.required(MetadataFields);
 fields=signal<string[]>([]);
 guideInfo=signal<{disponible:boolean;mensaje?:string;titulo?:string;referencia?:string;url?:string}|null>(null);
 message=signal('');error=signal('');busy=signal(false);duplicate=signal(false);
 meta=createMetadataDraft();control=emptyControls();file:File|null=null;reference='';retireReason='';link={url:'',tipo:'',descripcion:''};
 constructor(){this.load();}
 async run(fn:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');this.message.set('');try{await fn();}catch(e:any){this.error.set(e.message);if(e.details?.controles)this.checks.set(e.details.controles);if(e.details?.duplicate)this.duplicate.set(true);}finally{this.busy.set(false);}}
 async load(){try{this.items.set(await this.api.request('/admin/trabajos'));}catch(e:any){this.error.set(e.message);}}
 newWork(){this.current.set(null);this.meta=createMetadataDraft();this.control=emptyControls();this.checks.set([]);this.links.set([]);this.duplicate.set(false);this.fields.set([]);this.resetExampleInput();this.message.set('Nueva ficha. Guardá los datos para agregar el PDF.');this.error.set('');this.metadataFields().focusFirst();}
 async select(id:string){const t=await this.api.request('/admin/trabajos/'+id);this.current.set(t);this.checks.set(t.controles);this.duplicate.set(false);
 this.meta={titulo:t.titulo,autores:t.autores.join('; '),anio:t.anio,carrera:t.carrera,area:t.area,tipo:t.tipo,resumen:t.resumen};
 this.control={...emptyControls(),ejemplar_id:t.ejemplar_actual||'',deposito_referencia:t.deposito_referencia||'',deposito_fecha:t.deposito_fecha||'',deposito_verificado:t.deposito_verificado,conformidad_referencia:t.ejemplar?.conformidad_referencia||'',ficha_publica:t.ficha_publica,lectura:t.lectura,descarga:t.descarga,autorizacion:t.autorizacion?{...t.autorizacion,vence:t.autorizacion.vence||''}:emptyControls().autorizacion,decision:t.decision?{resultado:t.decision.resultado,motivo:t.decision.motivo}:emptyControls().decision};
 this.links.set(await this.api.request('/admin/trabajos/'+id+'/enlaces'));}
 open(id:string){this.run(async()=>{await this.select(id);this.fields.set([]);this.resetExampleInput();});}
 private showFields(fields:string[]){
  this.fields.set(fields);
  this.metadataFields().focusFirst(fields);
 }
 save(confirmarDuplicado=false){this.run(async()=>{
  this.duplicate.set(false);
  const data=metadataFromDraft(this.meta);
  const invalid=validateMetadata(data);
  this.showFields(invalid);
  if(invalid.length)throw new Error('Revisá los campos marcados.');
  const id=this.current()?.id;
  try{
   const t=await this.api.request('/admin/trabajos'+(id?'/'+id:''),id?'PUT':'POST',{...data,confirmarDuplicado});
   await this.load();
   await this.select(t.id);
   this.message.set('Metadatos guardados.');
  }catch(e:any){
   if(Array.isArray(e.details?.fields))this.showFields(e.details.fields);
   throw e;
  }
 });}
 private resetExampleInput(){
  this.file=null;
  this.reference='';
  this.retireReason='';
  const input=this.element.nativeElement.querySelector<HTMLInputElement>('#pdf-file');
  if(input)input.value='';
 }
 choose(event:Event){this.file=(event.target as HTMLInputElement).files?.[0]||null;}
 upload(){this.run(async()=>{
  if(!this.file)throw new Error('Seleccioná un PDF.');
  const data=new FormData();data.append('archivo',this.file);
  await this.api.request('/admin/trabajos/'+this.current().id+'/pdf','POST',data);
  this.resetExampleInput();
  await this.select(this.current().id);await this.load();
  this.message.set('PDF asociado. Completá sus verificaciones y permisos antes de publicar.');
 });}
 saveReference(){this.run(async()=>{
  await this.api.request('/admin/trabajos/'+this.current().id+'/referencia','POST',{referencia:this.reference});
  this.resetExampleInput();
  await this.select(this.current().id);await this.load();
  this.message.set('Referencia asociada. Registrá la conformidad del ejemplar.');
 });}
 saveControls(){this.run(async()=>{const b:any={...this.control};if(!b.autorizacion.referencia&&!b.autorizacion.otorgantes)delete b.autorizacion;if(!b.decision.motivo)delete b.decision;await this.api.request('/admin/trabajos/'+this.current().id+'/controles','PUT',b);await this.select(this.current().id);await this.load();this.message.set('Controles registrados para este ejemplar.');});}
 publish(){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/publicar','POST');await this.select(this.current().id);await this.load();this.message.set('Trabajo publicado en el catálogo.');});}
 retire(dialog:HTMLDialogElement){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/retirar','POST',{motivo:this.retireReason});dialog.close();await this.select(this.current().id);await this.load();this.retireReason='';this.message.set('Publicación retirada. Se conserva el registro administrativo.');});}
 activity(){this.run(async()=>{this.logs.set(await this.api.request('/admin/bitacora'));this.report.set(await this.api.request('/admin/reporte'));});}
 guide(){this.run(async()=>{this.guideInfo.set(await this.api.request('/admin/guia'));});}
 addLink(){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/enlaces','POST',this.link);this.link={url:'',tipo:'',descripcion:''};await this.select(this.current().id);this.message.set('Enlace guardado, deshabilitado de inicio.');});}
 toggleLink(l:any){this.run(async()=>{await this.api.request('/admin/enlaces/'+l.id,'PATCH',{habilitado:!l.habilitado});await this.select(this.current().id);});}
}
