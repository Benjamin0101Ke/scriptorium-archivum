const storageKey = 'scriptorium-archivum.demo.v1';
const sessionKey = 'scriptorium-archivum.demo.session';

export function readDemoData<T>(initial: () => T): T {
  try {
    const data = localStorage.getItem(storageKey);
    return data ? JSON.parse(data) : initial();
  } catch {
    throw new Error('No se pudieron recuperar los datos locales de este navegador.');
  }
}

export function saveDemoData(data: unknown): void {
  try {
    localStorage.setItem(storageKey, JSON.stringify(data));
  } catch {
    throw new Error(
      'No se pudieron guardar los cambios. Revisá el espacio y los permisos del navegador.',
    );
  }
}

export function demoSession(): string | null {
  return sessionStorage.getItem(sessionKey);
}

export function setDemoSession(id: string | null): void {
  if (id) sessionStorage.setItem(sessionKey, id);
  else sessionStorage.removeItem(sessionKey);
}

async function accessPdf<T>(
  mode: IDBTransactionMode,
  operation: (files: IDBObjectStore) => IDBRequest<T>,
): Promise<T> {
  const db = await new Promise<IDBDatabase>((resolve, reject) => {
    const request = indexedDB.open(storageKey, 1);
    request.onupgradeneeded = () => request.result.createObjectStore('pdfs');
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(new Error('No se pudo abrir el almacén local de PDF.'));
  });

  try {
    return await new Promise<T>((resolve, reject) => {
      const tx = db.transaction('pdfs', mode);
      const request = operation(tx.objectStore('pdfs'));
      tx.oncomplete = () => resolve(request.result);
      tx.onabort = () =>
        reject(new Error('No se pudo guardar o recuperar el PDF en este navegador.'));
    });
  } finally {
    db.close();
  }
}

export const readPdf = (id: string) =>
  accessPdf<Blob | undefined>('readonly', (files) => files.get(id));
export const savePdf = (id: string, pdf: Blob) =>
  accessPdf('readwrite', (files) => files.put(pdf, id));
export const deletePdf = (id: string) => accessPdf('readwrite', (files) => files.delete(id));
