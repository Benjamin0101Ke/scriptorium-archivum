import { DatePipe } from '@angular/common';
import { hoyElSalvador } from '../core/fechas';
import {Component,ElementRef,inject,signal,viewChild} from '@angular/core';import {FormsModule} from '@angular/forms';import {Api} from '../core/api';
import {createMetadataDraft,metadataFromDraft,validateMetadata} from '../core/metadata';
import {MetadataFields} from '../shared/metadata-fields';
import {LocalPdfPreview} from '../shared/local-pdf-preview';
import {PersonPicker} from '../shared/person-picker';
import {ArchivosTrabajo} from '../shared/archivos-trabajo';
import {cargarCatalogos, CatalogosBiblioteca, nuevaSeleccion, OpcionCatalogo, personasSeleccionadas} from '../core/biblioteca-catalogos';
const emptyControls=()=>({ejemplar_id:'',deposito_referencia:'',deposito_fecha:'',deposito_verificado:false,verificar:false,conformidad_referencia:'',ficha_publica:false,lectura:false,descarga:false,autorizacion:{referencia:'',otorgantes:'',fecha:hoyElSalvador(),vence:'',estado:'Vigente',ficha:false,lectura:false,descarga:false},registrar_decision:false,decision:{resultado:'Favorable',motivo:''}});
@Component({
  selector: 'app-biblioteca',
  imports: [DatePipe, FormsModule, MetadataFields, LocalPdfPreview, PersonPicker, ArchivosTrabajo],
  templateUrl: './biblioteca.html',
  styleUrl: './biblioteca.css'
})export class Biblioteca {
 api=inject(Api);items=signal<any[]>([]);current=signal<any>(null);checks=signal<any[]>([]);logs=signal<any[]>([]);report=signal<any[]>([]);links=signal<any[]>([]);
 private element=inject<ElementRef<HTMLElement>>(ElementRef);
 private metadataFields=viewChild.required(MetadataFields);
 fields=signal<string[]>([]);
 catalogos=signal<CatalogosBiblioteca|null>(null);
 catalogosLoading=signal(false);
 catalogosError=signal('');
 seleccion=nuevaSeleccion();
 otorgantes:OpcionCatalogo[]=[];
 guideInfo=signal<{disponible:boolean;mensaje?:string;titulo?:string;referencia?:string;url?:string}|null>(null);
 message=signal('');error=signal('');busy=signal(false);duplicate=signal(false);
 feedback=signal<'controles'|'publicar'|null>(null);
 readonly destinosControl:Record<string,string>={
  'Metadatos completos':'biblioteca-metadatos',
  'Comprobante UES verificado':'biblioteca-deposito',
  'Ejemplar final verificado':'biblioteca-conformidad',
  'Constancia vigente para el ejemplar':'biblioteca-constancia',
  'Decisión favorable de Biblioteca':'biblioteca-decision',
  'Ficha pública autorizada':'biblioteca-permisos',
  'Permisos de PDF respaldados':'biblioteca-permisos',
  'PDF disponible y controlado':'biblioteca-ejemplar',
  'Cuenta de Biblioteca y estado válido':'biblioteca-cuenta',
 };
 irAlControl(event:MouseEvent,label:string){
  event.preventDefault();
  const raiz=this.element.nativeElement;
  const destino=raiz.querySelector<HTMLElement>('#'+this.destinosControl[label])
   ??raiz.querySelector<HTMLElement>('#biblioteca-ejemplar');
  if(!destino)return;
  const campo=destino.querySelector<HTMLElement>('input:not([disabled]),select:not([disabled]),textarea:not([disabled])')??destino;
  campo.focus({preventScroll:true});
  destino.scrollIntoView({behavior:matchMedia('(prefers-reduced-motion: reduce)').matches?'instant':'smooth',block:'center'});
 }
 meta=createMetadataDraft();control=emptyControls();file:File|null=null;reference='';retireReason='';link={url:'',tipo:'',descripcion:''};
 private metaGuardada = this.metadataSnapshot();
 constructor(){
  this.load();
  if(!this.api.demo)this.loadCatalogos();
 }
 async loadCatalogos(){
  this.catalogosLoading.set(true);
  this.catalogosError.set('');
  try{
   this.catalogos.set(await cargarCatalogos(this.api));
  }catch(e:any){this.catalogosError.set(e.message);}
  finally{this.catalogosLoading.set(false);}
 }
 private metadataSnapshot(){return JSON.stringify({meta:this.meta,seleccion:this.seleccion});}
 private hayCambios(): boolean {
  return this.metadataSnapshot() !== this.metaGuardada;
}
private pendingAction: (() => void) | null = null;

solicitarConfirmacion(
  action: () => void,
  dialog: HTMLDialogElement
) {
  if (!this.hayCambios()) {
    action();
    return;
  }

  this.pendingAction = action;
  dialog.showModal();
}

resolverConfirmacion(
  descartar: boolean,
  dialog: HTMLDialogElement
) {
  dialog.close();

  const action = this.pendingAction;
  this.pendingAction = null;

  if (descartar) {
    action?.();
  }
}

 async run(fn:()=>Promise<void>,feedback:'controles'|'publicar'|null=null){if(this.busy())return;this.feedback.set(feedback);this.busy.set(true);this.error.set('');this.message.set('');try{await fn();}catch(e:any){this.error.set(e.message);if(e.details?.controles)this.checks.set(e.details.controles);if(e.details?.duplicate)this.duplicate.set(true);}finally{this.busy.set(false);}}
 async load(){try{this.items.set(await this.api.request('/admin/trabajos'));}catch(e:any){this.error.set(e.message);}}
newWork(dialog: HTMLDialogElement) {
  this.solicitarConfirmacion(() => {
    this.current.set(null);
    this.meta = createMetadataDraft();
    this.seleccion=nuevaSeleccion();
    this.otorgantes=[];
    this.metaGuardada = this.metadataSnapshot();
    this.control = emptyControls();
    this.checks.set([]);
    this.links.set([]);
    this.duplicate.set(false);
    this.fields.set([]);
    this.resetExampleInput();
    this.message.set('Nueva ficha. Guardá los datos para agregar el PDF.');
    this.error.set('');
    this.metadataFields().focusFirst();
  }, dialog);
}
 async select(id:string){const t=await this.api.request('/admin/trabajos/'+id);this.current.set(t);this.checks.set(t.controles);this.duplicate.set(false);
 this.meta={titulo:t.titulo,autores:t.autores.join('; '),anio:t.anio,carrera:t.carrera,area:t.area,tipo:t.tipo,resumen:t.resumen};
 this.seleccion={
  autores:personasSeleccionadas(t.autor_ids??[],t.autores),
  id_carrera:t.id_carrera??null,
  id_tipo_trabajo:t.id_tipo_trabajo??null,
  area_ids:t.area_ids??[],
 };
 this.otorgantes=personasSeleccionadas(
  t.autorizacion?.otorgante_ids??[],(t.autorizacion?.otorgantes??'').split(';').map((nombre:string)=>nombre.trim())
 );
 this.metaGuardada = this.metadataSnapshot();
 this.control={...emptyControls(),ejemplar_id:t.ejemplar_actual||'',deposito_referencia:t.deposito_referencia||'',deposito_fecha:t.deposito_fecha||'',deposito_verificado:t.deposito_verificado,conformidad_referencia:t.ejemplar?.conformidad_referencia||'',ficha_publica:t.ficha_publica,lectura:t.lectura,descarga:t.descarga,autorizacion:t.autorizacion?{...t.autorizacion,vence:t.autorizacion.vence||''}:emptyControls().autorizacion,decision:t.decision?{resultado:t.decision.resultado,motivo:t.decision.motivo}:emptyControls().decision};
 this.links.set(await this.api.request('/admin/trabajos/'+id+'/enlaces'));}
 open(id: string, dialog: HTMLDialogElement) {
  this.solicitarConfirmacion(() => {
    this.run(async () => {
      await this.select(id);
      this.fields.set([]);
      this.resetExampleInput();
    });
  }, dialog);
}
 private showFields(fields:string[]){
  this.fields.set(fields);
  this.metadataFields().focusFirst(fields);
 }
 save(confirmarDuplicado=false){this.run(async()=>{
  this.duplicate.set(false);
  const catalogos=this.catalogos();
  if(!this.api.demo&&!catalogos)throw new Error('Cargá los catálogos antes de guardar la ficha.');
  const {autores,id_carrera,id_tipo_trabajo,area_ids}=this.seleccion;
  const tipoConservado=this.current()?.id_tipo_trabajo===id_tipo_trabajo?this.current()?.tipo:'';
  const data=catalogos?{
   ...this.meta,
   autores:autores.map(persona=>persona.nombre),
   carrera:catalogos.carreras.find(carrera=>carrera.id===id_carrera)?.nombre??'',
   tipo:catalogos.tipos.find(tipo=>tipo.id===id_tipo_trabajo)?.nombre??tipoConservado??'',
   area:area_ids.map(id=>catalogos.areas.find(area=>area.id===id)?.nombre??'').join('; '),
  }:metadataFromDraft(this.meta);
  const invalid=validateMetadata(data);
  this.showFields(invalid);
  if(invalid.length)throw new Error('Revisá los campos marcados.');
  const id=this.current()?.id;
  const {titulo,anio,resumen}=data;
  const payload=this.api.demo?data:{
   titulo,anio,resumen,id_carrera,id_tipo_trabajo,area_ids,autor_ids:autores.map(persona=>persona.id),
  };
  try{
   const t=await this.api.request('/admin/trabajos'+(id?'/'+id:''),id?'PUT':'POST',{...payload,confirmarDuplicado});
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
  const id=this.current().id;
  const data=new FormData();data.append('archivo',this.file);
  const uploaded=await this.api.request<{ejemplar_id:string}>('/admin/trabajos/'+id+'/pdf','POST',data);
  this.resetExampleInput();
  await this.select(id);await this.load();
  if(this.control.ejemplar_id!==uploaded.ejemplar_id)
   throw new Error('El ejemplar cambió tras la carga. Revisá el ejemplar que figura ahora en la ficha.');
  this.message.set('PDF asociado. Completá sus verificaciones y permisos antes de publicar.');
 });}
 saveReference(){this.run(async()=>{
  await this.api.request('/admin/trabajos/'+this.current().id+'/referencia','POST',{referencia:this.reference});
  this.resetExampleInput();
  await this.select(this.current().id);await this.load();
  this.message.set('Referencia asociada. Registrá la conformidad del ejemplar.');
 });}
 changeDepositReference(value:string){
  if(value.trim()!==this.control.deposito_referencia.trim())this.control.deposito_verificado=false;
  this.control.deposito_referencia=value;
 }
 saveControls(){this.run(async()=>{
  const id=this.current().id;
  const {autorizacion,decision,registrar_decision,...controls}=this.control;
  if(registrar_decision&&!decision.motivo.trim())throw new Error('Ingresá el motivo de la nueva decisión.');
  const {otorgantes,...constancia}=autorizacion;
  const tieneOtorgantes=this.api.demo?!!otorgantes.trim():this.otorgantes.length>0;
  const registrarConstancia=!!constancia.referencia.trim()||tieneOtorgantes||constancia.estado==='Revocada';
  if(registrarConstancia&&!constancia.fecha)throw new Error('Indicá la fecha de otorgamiento de la constancia.');
  if(registrarConstancia&&constancia.vence&&constancia.vence<constancia.fecha)throw new Error('La fecha de vencimiento no puede ser anterior a la fecha de otorgamiento.');
  const payload={
   ...controls,
   registrar_decision,
   autorizacion:registrarConstancia?(this.api.demo?autorizacion:{...constancia,otorgante_ids:this.otorgantes.map(persona=>persona.id)}):undefined,
   decision:registrar_decision?decision:undefined,
  };
  await this.api.request('/admin/trabajos/'+id+'/controles','PUT',payload);
  await this.select(id);await this.load();
  this.message.set('Controles registrados para este ejemplar.');
 },'controles');}
 publish(){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/publicar','POST');await this.select(this.current().id);await this.load();this.message.set('Trabajo publicado en el catálogo.');},'publicar');}
 retire(dialog:HTMLDialogElement){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/retirar','POST',{motivo:this.retireReason});dialog.close();await this.select(this.current().id);await this.load();this.retireReason='';this.message.set('Publicación retirada. Se conserva el registro administrativo.');});}
 activity(){this.run(async()=>{
  const logs=await this.api.request<any[]>('/admin/bitacora');
  this.logs.set(logs);
  if(!logs.length)this.message.set('Todavía no hay actuaciones registradas.');
 });}
 annualReport(){this.run(async()=>{
  try{
   const report=await this.api.request<any[]>('/admin/reporte');
   this.report.set(report);
   if(!report.length)this.message.set('Todavía no hay datos para el reporte anual.');
  }catch(e:any){
   if(e.status!==404&&e.status!==501)throw e;
   this.report.set([]);
   this.message.set('El reporte anual todavía no está disponible.');
  }
 });}
 guide(){this.run(async()=>{this.guideInfo.set(await this.api.request('/admin/guia'));});}
 addLink(){this.run(async()=>{await this.api.request('/admin/trabajos/'+this.current().id+'/enlaces','POST',this.link);this.link={url:'',tipo:'',descripcion:''};await this.select(this.current().id);this.message.set('Enlace guardado, deshabilitado de inicio.');});}
 toggleLink(l:any){this.run(async()=>{await this.api.request('/admin/enlaces/'+l.id,'PATCH',{habilitado:!l.habilitado});await this.select(this.current().id);});}
private selectedLink: any = null;

deleteLink(l: any, dialog: HTMLDialogElement) {
  this.selectedLink = l;
  dialog.showModal();
}

confirmDeleteLink(dialog: HTMLDialogElement) {
  const l = this.selectedLink;
  if (!l) return;

  dialog.close();
  this.selectedLink = null;

  this.run(async () => {
    await this.api.request(
      '/admin/trabajos/' + this.current().id + '/enlaces/' + l.id,
      'DELETE'
    );

    await this.select(this.current().id);
    this.message.set('Enlace eliminado correctamente.');
  });
}
}
