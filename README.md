# Scriptorium Archivum

Interfaz Angular del Repositorio Digital de Proyectos y Trabajos de Grado de la UES, Facultad Multidisciplinaria Oriental. Parte de **Benjamín Daniel Sánchez Sorto (SS24024)**.

## Abrir el proyecto en tu computadora

Usá Node.js 24.15 o posterior de la rama 24, o Node.js 26. Abrí PowerShell en esta copia del proyecto y ejecutá:

```powershell
cd frontend
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4300
```

Abrí [la demostración local](http://127.0.0.1:4300/). Mantené abierta la terminal mientras la usás. Para detenerla, presioná Ctrl+C. En arranques posteriores podés volver a ejecutar solamente `npm.cmd start -- --host 127.0.0.1 --port 4300`.

El HTML está en `frontend/src/app`. Abrí las páginas desde el servidor local de Angular, que compila sus plantillas y resuelve la navegación.

## Probar la demostración

| Vista | Correo | Contraseña de prueba |
| --- | --- | --- |
| Biblioteca | biblioteca@demo.local | demo1234 |
| Lector | lector@demo.local | demo1234 |

También podés crear una cuenta lectora desde **Iniciar sesión → Crear cuenta lectora**. Usá datos ficticios. Cada cuenta tiene sus propios favoritos y envíos.

### Enviar un trabajo como lector

En la portada elegí **Agregar tesis o proyecto**. Ingresá como lector o creá una cuenta lectora local, completá los metadatos y adjuntá un PDF de prueba. El trabajo queda **En revisión** en **Mis envíos**. Desde Biblioteca, abrí la ficha en **Fichas registradas** y elegí **Revisar PDF** para ver el ejemplar. El envío local requiere los controles habituales de Biblioteca para publicarse y se limita a la demostración.

### Registrar y revisar desde Biblioteca

1. Entrá como Biblioteca y elegí **Agregar tesis o proyecto**. Completá los siete metadatos y guardá.
2. Asociá un PDF de prueba de hasta 25 MB o registrá la referencia institucional del ejemplar.
3. Registrá la evidencia del depósito, la conformidad, la constancia, los permisos y la decisión. Para difundir la ficha, marcá esa opción en la constancia y en los permisos que se habilitarán.
4. Guardá los controles y seleccioná **Comprobar y publicar**. Revisá la ficha desde el catálogo. Si asociaste un PDF, probá los botones de lectura y descarga que habilitaste.
5. Usá **Retirar del catálogo** para registrar un motivo y retirar una publicación.

## Dónde se guardan los cambios

- `localStorage` guarda fichas, controles, enlaces, actividad, cuentas de prueba y favoritos.
- `IndexedDB` guarda los PDF. La sesión de prueba se mantiene en la pestaña al recargar.
- Volvé a abrir el mismo navegador, perfil y dirección (`http://127.0.0.1:4300`) para recuperar los datos. Si borrás los datos del sitio, también se borran sus fichas y PDF. Otro puerto, navegador o perfil tiene su propio almacenamiento.

El registro y guardado locales sirven para la demostración en esta computadora. Usá el entorno integrado del equipo para cuentas y archivos institucionales.

## Código e integración

| Archivo | Contenido |
| --- | --- |
| `frontend/src/app/pages/*.html` | Formularios y pantallas |
| `frontend/src/styles.css` | Estilos y adaptación |
| `frontend/src/app/core/metadata.ts` | Validación de los siete metadatos |
| `frontend/src/app/core/api.ts` | Cliente de API y sesión |
| `frontend/src/app/core/demo.ts` | Conexión del cliente de API con los módulos locales |
| `frontend/src/app/core/demo/cuentas.ts` | Registro de lectores, ingreso y sesión de prueba |
| `frontend/src/app/core/demo/catalogo.ts` | Búsqueda, favoritos y consulta de fichas y PDF |
| `frontend/src/app/core/demo/biblioteca.ts` y `publicacion.ts` | Administración de fichas, ejemplares y controles de publicación |
| `frontend/src/app/pages/envios.ts` y `frontend/src/app/core/demo/envios.ts` | Envío local de trabajos y seguimiento por lector |
| `frontend/src/app/core/demo/almacenamiento.ts` y `datos.ts` | Persistencia en el navegador y datos de demostración |

La compilación de producción selecciona `settings.production.ts` y usa la API en `/api`. Para conectar el entorno de desarrollo con la API de Roberto, acordá el [contrato propuesto](docs/CONTRATO-API-PROPUESTO.md), poné `demo:false` en `frontend/src/app/core/settings.ts` y configurá el destino de `frontend/proxy.conf.json`.

Desde `frontend` se pueden ejecutar `npm.cmd test` y `npm.cmd run build`. Consultá [qué está pendiente](docs/TU-PARTE-Y-PENDIENTES.md) y [las verificaciones](docs/VERIFICACION.md).
