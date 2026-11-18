# Scriptorium Archivum

Repositorio Digital de Proyectos y Trabajos de Grado de la UES, Facultad Multidisciplinaria Oriental. Incluye la interfaz Angular en `frontend/` y la API ASP.NET Core con PostgreSQL en `backend/`.

## Abrir el proyecto en tu computadora

Para empezar desde cero, seguí la [instalación paso a paso con Docker, PostgreSQL y GitHub](docs/INSTALACION-PASO-A-PASO.md).

Con Docker Desktop activo, seguí la [guía de PostgreSQL y pgAdmin](backend/README.md). Ahí están los pasos para crear la configuración local, levantar los contenedores, cargar los datos ficticios y registrar la conexión en pgAdmin.

Con la API activa, usá Node.js 24.15 o posterior de la rama 24, o Node.js 26. Desde la raíz del repositorio:

```powershell
cd frontend
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4301
```

Abrí [la versión conectada](http://127.0.0.1:4301/). La API debe estar activa. Mantené abierta la terminal mientras la usás y presioná Ctrl+C para detener Angular.

El HTML está en `frontend/src/app`. Abrí las páginas desde el servidor local de Angular, que compila sus plantillas y resuelve la navegación.

## Probar Angular con la API de Roberto

1. En `backend/`, creá tu `.env` local a partir de `.env.example` y ejecutá `docker compose -p scriptorium-integracion up -d --build`.
2. Cargá los datos ficticios una sola vez sobre la base vacía, siguiendo la [guía del backend](backend/README.md). Debe aparecer una carrera en `/api/carreras` y `/api/trabajos` devuelve `[]` mientras todas las fichas sigan pendientes.
3. Desde `frontend/`, iniciá Angular con esta configuración:

```powershell
npm.cmd start -- --configuration integration --host 127.0.0.1 --port 4301
```

Abrí [la versión conectada](http://127.0.0.1:4301/). En la pestaña **Red** del navegador, las solicitudes deben salir a `http://127.0.0.1:4301/api/…`. El proxy de Angular las reenvía a `127.0.0.1:3000` y el navegador mantiene la sesión por cookie HttpOnly. Si cambiás `API_PORT` en el backend, ajustá el destino en `frontend/proxy.conf.json` y reiniciá Angular.

| Rol | Correo ficticio | Contraseña de integración |
| --- | --- | --- |
| Biblioteca | biblioteca@scriptorium.test | Practica-Biblioteca-2026! |
| Lector | lector@scriptorium.test | Practica-Lector-2026! |

**Inicio de sesión y catálogo.** Entrá con una cuenta de prueba y verificá que se abra la página de su rol. Al recargar, la sesión debe seguir activa. El catálogo público debe mostrar solo los trabajos publicados con difusión autorizada.

| Rol | Página de inicio | Qué probar |
| --- | --- | --- |
| Lector | `/lector` | Abrí el catálogo, guardá una ficha en Favoritos y entrá a Mis envíos. |
| Estudiante | `/estudiante` | Entrá a Mis envíos, adjuntá un PDF de prueba y revisá el estado del envío. |
| Docente revisor | `/docente` | Consultá publicaciones y favoritos. La revisión académica se gestiona fuera del repositorio, según la v3. |
| Biblioteca | `/biblioteca` | Abrí Gestión de información y revisá el ejemplar pendiente. |
| Administrador | `/admin/roles` | Asigná un rol a una cuenta ficticia. Iniciá sesión con esa cuenta para ver su espacio. |

**Registro y perfil.** En **Iniciar sesión → Crear cuenta**, elegí departamento, carrera y la versión del plan disponible. Elegir carrera crea una cuenta Estudiante. Sin carrera se registra una cuenta Lector. El correo aparecerá en `public.usuarios` de PostgreSQL. En **Mi perfil** podés cambiar nombre visible, foto, avatar, tema, carrera, plan y contraseña. Una cuenta lectora que completa su carrera pasa a Estudiante. **Mis envíos** guarda la ficha en PostgreSQL y el PDF en el almacenamiento del backend para revisión de Biblioteca.

**Administración.** El panel permite buscar cuentas, filtrar por rol o estado, habilitar o deshabilitar cuentas, asignar roles, solicitar recuperación y consultar actividad. En **Gestionar carreras y departamentos** se administran las carreras, departamentos, áreas temáticas y versiones del pénsum. La carga inicial incluye 41 carreras de la [oferta oficial UES FMO](https://eel.ues.edu.sv/ingreso/index/contenido/600), organizadas en nueve áreas. Las versiones concretas del plan se agregan con su referencia oficial.

**Documentos y adjuntos.** **Leer PDF** abre el visor dentro de la ficha pública. Biblioteca y la persona autora pueden adjuntar Word, Excel, ZIP, PDF y archivos de código o texto en **Archivos complementarios**. Biblioteca habilita la difusión de cada archivo, que también requiere el permiso de descarga del trabajo para su acceso público.

**Catálogo y valoraciones.** El catálogo muestra hasta 12 trabajos por página y calcula los controles según los resultados. Desde la ficha, cada cuenta autenticada puede guardar, actualizar o quitar su valoración de 1 a 5 estrellas. El promedio y la cantidad de votos determinan el orden predeterminado. Los trabajos sin valoraciones mantienen el diseño inicial. Los trabajos valorados usan dorado para 5, plateado desde 4.5, bronce desde 4, turquesa desde 3, violeta desde 2 y coral por debajo de 2. Las valoraciones se guardan en PostgreSQL con la migración 10.

**Tipos de trabajo.** En **Roles → Gestionar catálogos → Tipos de trabajo**, Administración agrega, renombra o desactiva tipos. La migración 11 incluye Trabajo de grado, Tesis, Proyecto de investigación, Proyecto de software, Informe técnico y Monografía. Los tipos activos aparecen en Mis envíos y Biblioteca. El código fuente se puede adjuntar como archivo complementario.

**Recuperación y roles.** Probá **Olvidé mi contraseña** con la [bandeja local de correo](http://127.0.0.1:8025/). La guía del backend explica cómo habilitar la primera cuenta administradora para ver el menú **Roles** y cómo importar la [colección de Postman](backend/pruebas/Scriptorium.postman_collection.json).

**Biblioteca y ejemplares.** Elegí personas registradas y los catálogos del servidor al guardar la ficha. Se puede seleccionar más de un área. Usá `backend/pruebas/ejemplar-ficticio.pdf` para asociar el ejemplar A y abrilo con **Revisar PDF** antes de publicarlo. Completá el depósito, la verificación y la constancia, habilitá los permisos concedidos y marcá **Registrar una decisión nueva** cuando corresponda. Cambiar el correlativo del depósito exige volver a confirmar su verificación. Si solo corregís otro control, dejá desmarcada la opción de decisión nueva.

Publicá A y reemplazalo por B. Esperá un ID distinto y una ficha **En revisión** con la decisión pendiente. La vista previa anterior debe devolver `404` y cualquier intento de guardar controles con el ID de A debe devolver `409`. **Actividad** consulta la bitácora por separado de **Reporte anual**.

## Probar la demostración

Para abrir la demo, ejecutá `npm.cmd start -- --configuration demo --host localhost --port 4302`. Sus cuentas y envíos se guardan en el navegador. La configuración predeterminada de Angular utiliza la API real.

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

## Dónde se guardan los datos

En el modo conectado, PostgreSQL conserva las cuentas, los perfiles y las fichas en un volumen de Docker. Los PDF se guardan en `backend/storage/trabajos/` cuando se arranca Compose desde este repositorio. El navegador conserva la sesión mediante una cookie y recuerda el tema.

### Almacenamiento de la demo

- `localStorage` guarda fichas, controles, enlaces, actividad, cuentas de prueba y favoritos.
- `IndexedDB` guarda los PDF. La sesión de prueba se mantiene en la pestaña al recargar.
- Volvé a abrir el mismo navegador, perfil y dirección (`http://localhost:4302` en el ejemplo de la demo) para recuperar los datos. Si borrás los datos del sitio, también se borran sus fichas y PDF. Otro puerto, navegador o perfil tiene su propio almacenamiento.

El registro y guardado locales sirven para la demostración en esta computadora. Usá el entorno integrado del equipo para cuentas y archivos institucionales.

## Qué se comparte por GitHub

El repositorio incluye código, migraciones SQL, datos ficticios y `backend/.env.example`. Cada integrante crea su propio `backend/.env`. Git excluye los archivos `.env` efectivos, los respaldos y el almacenamiento de documentos. Las contraseñas de prueba de las guías solo se usan con los datos ficticios en el entorno local.

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

Las configuraciones `integration` y `production` seleccionan `settings.production.ts` y usan la API en `/api`. El comando `npm.cmd start` usa `integration` por defecto. Para desplegar la aplicación, el servidor web debe reenviar `/api` al backend bajo el mismo origen.

Desde `frontend` se pueden ejecutar `npm.cmd test` y `npm.cmd run build`. Consultá [qué está pendiente](docs/TU-PARTE-Y-PENDIENTES.md) y [las verificaciones](docs/VERIFICACION.md).

## Avisos automáticos

Los correos de recuperación llevan el logo del proyecto. Los eventos de cuenta y trabajo generan avisos para sus responsables, con historial de entrega en Administración. Consultá la [guía de correos y asignación de docentes](docs/AVISOS-CORREO.md).
