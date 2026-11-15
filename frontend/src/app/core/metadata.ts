export interface Metadata {titulo:string;autores:string[];anio:number;carrera:string;area:string;tipo:string;resumen:string}
export type MetadataDraft = Omit<Metadata, 'autores'> & { autores: string };

export const createMetadataDraft = (autores = ''): MetadataDraft => ({
 titulo: '', autores, anio: new Date().getFullYear(), carrera: '', area: '', tipo: 'Trabajo de grado', resumen: '',
});

export const metadataFromDraft = (draft: MetadataDraft): Metadata => ({
 ...draft, autores: draft.autores.split(';').map(author => author.trim()).filter(Boolean),
});

export function validateMetadata(value:Metadata):string[] {
 const missing:string[]=[];
 for(const field of ['titulo','carrera','area','tipo','resumen'] as const)if(!value[field]?.trim())missing.push(field);
 if(!value.autores.length||value.autores.some(a=>!a.trim()))missing.push('autores');
 if(!/^\d{4}$/.test(String(value.anio)))missing.push('anio');
 return missing;
}
