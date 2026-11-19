// La demostración local usa los mismos puntos de entrada que el cliente de API.
import { readDemoData, saveDemoData } from './demo/almacenamiento';
import { handleLibrary } from './demo/biblioteca';
import { handleCatalog } from './demo/catalogo';
import { currentUser, handleAccount, requireUser } from './demo/cuentas';
import { initialData } from './demo/datos';
import { handleSubmissions } from './demo/envios';

export function demoRequest(path: string, method: string, body: any): Promise<any> {
  // Serializar las operaciones coordina los datos y archivos entre pestañas.
  return navigator.locks.request('scriptorium-archivum.demo', async () => {
    const url = new URL(path, 'https://demo.invalid');
    const data = readDemoData(initialData);
    const save = () => saveDemoData(data);
    if (url.pathname.startsWith('/auth/'))
      return handleAccount(url.pathname, data.accounts, body, save);
    const user = currentUser(data.accounts);
    if (url.pathname === '/mis-envios')
      return handleSubmissions(method, body, data, requireUser(user), save);
    if (url.pathname.startsWith('/admin/'))
      return handleLibrary(url.pathname, method, body, data, requireUser(user), save);
    return handleCatalog(url, method, data, user, save);
  });
}
