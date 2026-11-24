import { deletePdf, savePdf } from './almacenamiento';
import { error } from './datos';

export async function validatePdf(value: FormDataEntryValue | null): Promise<File> {
  if (
    !(value instanceof File) ||
    value.size > 6 * 1024 * 1024 * 1024 ||
    !value.name.toLowerCase().endsWith('.pdf') ||
    (await value.slice(0, 5).text()) !== '%PDF-'
  ) {
    error('Selecciona un PDF válido de hasta 6 GB.');
  }
  return value;
}

export async function persistPdf(id: string, file: File | null, save: () => void) {
  if (file) await savePdf(id, new Blob([file], { type: 'application/pdf' }));
  try {
    save();
  } catch (e) {
    if (file) await deletePdf(id).catch(() => undefined);
    throw e;
  }
}
