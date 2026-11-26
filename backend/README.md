# Base de datos y API de Scriptorium Archivum

Esta carpeta contiene la API ASP.NET Core con Entity Framework Core, los scripts de PostgreSQL y Docker Compose. La interfaz utiliza las rutas existentes de la API; no es necesario crear nuevamente los controladores.

## Estructura

| Carpeta o archivo | Función |
| --- | --- |
| `APIScriptoriumArchivum/` | Código de la API, modelos, contexto, permisos y servicios |
| `sql-init/` | Esquema y migraciones, ordenados por número y nombre |
| `compose.yaml` | Servicios PostgreSQL 17, API, pgAdmin y Mailpit |
| `storage/trabajos/` | PDF y archivos complementarios de la instalación |
| `documentacion/` | DER, modelo relacional, editables y diccionario de datos |
| `integracion/` | Contratos y explicaciones para consumir la API |
| `pruebas/` | Colección de Postman, ejemplos y comprobaciones |

## Iniciar una instalación nueva

Con Docker Desktop iniciado, abrir PowerShell en la carpeta `backend`:

```powershell
if (!(Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
notepad .env
```

Completar `POSTGRES_PASSWORD` y `PGADMIN_DEFAULT_PASSWORD` con las contraseñas de la instalación. `API_PROJECT_PATH` vacío utiliza el proyecto de esta carpeta. El archivo `.env` contiene la configuración privada y no se publica; `.env.example` es la plantilla sin secretos.

```powershell
docker compose -p scriptorium-integracion up -d --build
docker compose -p scriptorium-integracion ps
docker compose -p scriptorium-integracion exec -T postgres pg_isready -h 127.0.0.1 -U postgres -d scriptorium_archivum
```

El último comando comprueba si PostgreSQL acepta conexiones dentro de su contenedor. `-U` selecciona el usuario y `-d` la base. No verifica por sí solo las tablas ni la API. Los scripts de `sql-init` se ejecutan automáticamente solo al inicializar un volumen vacío.

| Servicio | Dirección predeterminada |
| --- | --- |
| API | `http://127.0.0.1:3000` |
| PostgreSQL desde Windows | `127.0.0.1:5433` |
| pgAdmin | `http://127.0.0.1:5052` |
| Mailpit | `http://127.0.0.1:8025` |

Para registrar el servidor en pgAdmin, usar host `postgres`, puerto `5432`, base `scriptorium_archivum`, usuario `postgres` y la contraseña local de PostgreSQL. El acceso a pgAdmin utiliza su correo y contraseña configurados en `.env`.

Una consulta `GET /api/carreras` permite comprobar la respuesta de la API. La raíz `/` puede devolver 404 porque no corresponde a un endpoint definido. El catálogo público queda vacío hasta que Biblioteca publique una ficha con los controles requeridos.

## Conectar Angular

Desde la carpeta `frontend`, que contiene `package.json`:

```powershell
npm ci
npm start -- --configuration integration --host 127.0.0.1 --port 4301
```

`npm` administra las dependencias del proyecto JavaScript. `npm ci` instala las versiones del archivo de dependencias bloqueadas y `npm start` ejecuta el arranque definido en `package.json`. La configuración `integration` utiliza el proxy para reenviar `/api` a la API local. Si cambia el puerto de la API, también debe ajustarse el destino de ese proxy. Después de modificar el proxy se reinicia el proceso de Angular.

Consultar el [contrato de integración](integracion/CONSUMIR-API.txt). Las peticiones autenticadas conservan la cookie de sesión; las rutas, métodos y formatos deben respetar el contrato.

## Agregar datos de prueba, opcional

El script `datos-prueba.sql` se utiliza una sola vez y únicamente en una base de práctica sin usuarios ni fichas. No se ejecuta automáticamente con `sql-init`. La [prueba guiada](pruebas/PRUEBA-GUIADA.txt) explica el recorrido de sesión, ficha, ejemplar y publicación. La [colección de Postman](pruebas/Scriptorium.postman_collection.json) permite comprobar las rutas.

## Utilizar una base existente

Conservar el mismo nombre de proyecto Compose, carpeta de almacenamiento y volúmenes. Antes de aplicar las migraciones pendientes, realizar un respaldo privado y pausar las escrituras. No ejecutar `down -v`, no repetir datos de prueba y no volver a crear las tablas sobre una base utilizada.

Aplicar únicamente las migraciones pendientes en orden de número y nombre. Hay números repetidos porque corresponden a ampliaciones distintas. Algunos scripts incluyen comandos de `psql`, como `\connect`; no se pegan sin adaptar en Query Tool. Reiniciar un contenedor no aplica migraciones nuevas sobre un volumen existente.

## Esquema y documentación

El esquema final contiene **34 tablas, 260 campos, 64 claves foráneas, una vista, cinco funciones, tres procedimientos y dos triggers**. Las 33 tablas del sistema se acompañan de `configuracion_correo_bitacora`, que conserva la configuración técnica de la copia institucional. No tiene claves foráneas.

- [Diccionario de datos](documentacion/Diccionario%20de%20datos%20actualizado%20-%20Scriptorium_archivum.pdf)
- [DER en PDF](documentacion/DER%20revisado%20-%20Scriptorium_archivum.pdf) y [editable de Draw.io](documentacion/DER%20actualizado%20-%20Scriptorium_archivum.drawio)
- [Modelo relacional en PDF](documentacion/Modelo%20relacional%20corregido%20-%20Scriptorium_archivum.pdf) y [editable de Draw.io](documentacion/Modelo%20relacional%20actualizado%20-%20Scriptorium_archivum.drawio)

Los PDF y editables incluyen dos láminas: el modelo principal y la configuración técnica de correo. La vista `vw_estrellas_por_trabajo` se representa por separado y no se cuenta como tabla.

## Favoritos y estrellas

Cada usuario puede mantener una marca por trabajo. Activar el favorito suma una estrella; desactivarlo conserva el registro sin contarlo. Las estrellas son la cantidad de favoritos activos, no una puntuación ni un promedio. La vista agrupa los favoritos activos por trabajo; la API presenta cero cuando el trabajo no aparece en ella.

## Publicación y correcciones

La revisión docente no publica trabajos. Biblioteca verifica el ejemplar, sus evidencias, la constancia, los otorgantes y los permisos antes de publicar. Una versión corregida conserva la anterior y vuelve a revisión sin heredar sus autorizaciones. Revocar una constancia exige que esté registrada y conserva su referencia y otorgantes.

## Correo y reintentos

Mailpit captura los correos de desarrollo y permite consultarlos en su interfaz local; no los entrega a destinatarios externos. El envío real utiliza `SMTP_HOST`, `SMTP_PORT`, `SMTP_TLS`, `SMTP_FROM`, `SMTP_USER` y `SMTP_PASSWORD` en la configuración privada.

`correos_eventos` mantiene la cola persistente de avisos y `correos_intentos` su historial. Los fallos se reintentan hasta el límite por ciclo; un reenvío autorizado conserva el historial anterior. Enviado significa aceptación por SMTP, no lectura por el destinatario.

La recuperación de contraseña utiliza un código protegido, de diez minutos y un solo uso, con una cola en memoria distinta de los avisos persistentes. Un reinicio puede perder un envío pendiente de esa cola.

La copia institucional se habilita de forma privada en `configuracion_correo_bitacora`. Un trigger diferido en `bitacora` encola las copias de eventos nuevos; una reversión de la transacción descarta también esa copia. La [configuración de copias institucionales](../docs/COPIAS-INTERNAS-20261125.md) explica su alcance.

## Respaldo y archivos privados

El [procedimiento de respaldo y restauración](pruebas/RESPALDO.txt) cubre la base y sus archivos. El respaldo se verifica en una instalación aislada. No se publican `.env`, credenciales SMTP, archivos reales de usuarios, claves de sesiones ni copias de la base.

Para detener y retomar sin borrar los datos:

```powershell
docker compose -p scriptorium-integracion stop
docker compose -p scriptorium-integracion start
```
