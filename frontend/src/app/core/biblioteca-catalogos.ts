export interface OpcionCatalogo {
  id: number;
  nombre: string;
}

export interface CatalogosBiblioteca {
  carreras: OpcionCatalogo[];
  tipos: OpcionCatalogo[];
  areas: OpcionCatalogo[];
}

export interface SeleccionBiblioteca {
  autores: OpcionCatalogo[];
  id_carrera: number | null;
  id_tipo_trabajo: number | null;
  area_ids: number[];
}

export const nuevaSeleccion = (): SeleccionBiblioteca => ({
  autores: [],
  id_carrera: null,
  id_tipo_trabajo: null,
  area_ids: [],
});

export function personasSeleccionadas(ids: number[], nombres: string[]): OpcionCatalogo[] {
  return ids.map((id, i) => ({ id, nombre: nombres[i] || `Persona ${id}` }));
}
import type { Api } from './api';


export async function cargarCatalogos(api: Api): Promise<CatalogosBiblioteca> {
  const [carreras, tipos, areas] = await Promise.all([
    api.request<OpcionCatalogo[]>('/carreras'),
    api.request<OpcionCatalogo[]>('/tipos-de-trabajo'),
    api.request<OpcionCatalogo[]>('/areas-tematicas'),
  ]);
  return { carreras, tipos, areas };
}
