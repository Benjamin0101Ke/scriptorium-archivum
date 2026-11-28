# Base de datos y API de Scriptorium Archivum

La API ASP.NET Core utiliza Entity Framework Core y PostgreSQL. Docker Compose inicia PostgreSQL y pgAdmin. La API se ejecuta desde Visual Studio o con `dotnet run`; Angular consume sus rutas `/api`.

## Archivos

Consultar el [análisis de uso de las 34 tablas](documentacion/USO-DEL-ESQUEMA.md) para distinguir operaciones actuales, módulos por comprobar y estructuras de fase futura.

| Archivo o carpeta | Uso |
| --- | --- |
| `APIScriptoriumArchivum/` | API, modelos, contexto, permisos y servicios |
| `docker-compose.yml` | PostgreSQL 17 y pgAdmin |
| `.env.example` | Muestra con explicaciones para preparar el archivo privado `.env` |
| `sql-init/` | SQL ordenados por numero y nombre |
| `storage/trabajos/` | PDF y archivos complementarios |
| `documentacion/` | Diagramas, editables y diccionario |
| `integracion/CONSUMIR-API.txt` | Rutas, sesion y formatos de la API |
| `pruebas/` | Coleccion de Postman, ejemplos y respaldo |

## 1. Preparar la configuracion

Se necesita Docker Desktop iniciado, .NET 10 y Visual Studio con soporte para ASP.NET Core, o la terminal de .NET. Para la interfaz se necesita Node.js compatible con `frontend/package.json`.

Abrir PowerShell en `backend`. Copiar `.env.example` como `.env` solamente si no existe un archivo privado:

```powershell
if (!(Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
notepad .env
```

Completar las contrasenas de PostgreSQL y pgAdmin. Cada campo tiene una explicacion. Para enviar correos, sustituir los correos de muestra, completar la contrasena de aplicacion y poner `SMTP_HABILITADO=true`. Con `false` se puede iniciar la API sin enviar mensajes; la recuperacion de contrasena por correo necesita el envio habilitado.

## 2. Iniciar PostgreSQL y pgAdmin

```powershell
docker compose -p scriptorium-archivum up -d postgres pgadmin
docker compose -p scriptorium-archivum ps
docker compose -p scriptorium-archivum exec -T postgres pg_isready -h 127.0.0.1 -U postgres -d scriptorium_archivum
```

`up -d` inicia los servicios en segundo plano. `ps` muestra su estado. `pg_isready` comprueba si PostgreSQL acepta conexiones; no verifica todas las tablas ni la API. `-h` indica el servidor dentro del contenedor, `-U` el usuario y `-d` la base.

El nombre despues de `-p` identifica la instalacion. Si ya existe una instalacion con otro nombre, conservar ese nombre en todos los comandos para utilizar sus volumenes.

Los SQL de `sql-init` se ejecutan automaticamente cuando PostgreSQL inicializa un volumen vacio. Si aparece un error de inicializacion, revisar el registro de PostgreSQL antes de continuar.

pgAdmin se abre en `http://localhost:5052`. Para registrar PostgreSQL desde pgAdmin, usar host `postgres`, puerto `5432`, usuario `postgres` y base `scriptorium_archivum`. Su contrasena es `POSTGRES_PASSWORD`. El acceso a la propia pagina de pgAdmin utiliza `PGADMIN_DEFAULT_EMAIL` y `PGADMIN_DEFAULT_PASSWORD`.

## 3. Ejecutar la API

Abrir el proyecto de `APIScriptoriumArchivum` en Visual Studio. Abrir una terminal en la carpeta que contiene su archivo `.csproj`.

```powershell
dotnet user-secrets set "ArchivoEntorno" "../.env"
dotnet user-secrets set "Storage:Ruta" "C:\ruta\scriptorium-archivum\backend\storage\trabajos"
```

La primera ruta se interpreta desde la carpeta del proyecto y selecciona el archivo privado del backend. Sustituir la segunda por la ruta absoluta de `backend/storage/trabajos` en ese equipo. Los secretos locales quedan fuera del repositorio. Si hay varias copias del proyecto en el mismo usuario, pueden compartir estos secretos; cada copia debe seleccionar sus propias rutas antes de arrancar.

Seleccionar el perfil `http` y ejecutar con F5 o Ctrl F5. Desde la misma carpeta tambien se puede ejecutar:

```powershell
dotnet run --launch-profile http
```

Abrir `http://localhost:3000/api/carreras`. La raiz `/` puede responder 404 porque no tiene una pagina definida. El catalogo publico queda vacio hasta que Biblioteca publique un trabajo con sus controles completos.

`Program.cs` llama a `EntornoPrivado.Cargar`. Este lector toma la contrasena y el puerto del `.env` y completa `ConnectionStrings:Scriptorium` de `appsettings.json`. Npgsql y Entity Framework utilizan esa cadena para conectarse a PostgreSQL en `localhost:5433`. La API carga sus datos privados por su propio codigo; Compose utiliza los suyos para iniciar los contenedores.

No escribir claves en `appsettings.json`, `launchSettings.json` ni en el codigo. Reiniciar la API despues de cambiar el `.env`.

## 4. Conectar Angular

Desde `frontend`:

```powershell
npm ci
npm start -- --configuration integration --host 127.0.0.1 --port 4301
```

`npm` administra los paquetes de Node.js. `npm ci` instala las versiones registradas en `package-lock.json`. `npm start` ejecuta el arranque definido en `package.json`; `integration` selecciona la conexion real. Abrir `http://127.0.0.1:4301/` y mantener la terminal abierta.

El proxy `frontend/proxy.conf.json` reenvia `/api` a `http://127.0.0.1:3000`. Si se cambia el puerto de la API en `Properties/launchSettings.json`, ajustar el destino del proxy y reiniciar Angular. El [contrato de integracion](integracion/CONSUMIR-API.txt) explica los metodos, formatos y la cookie de sesion.

## 5. Datos y archivos

El esquema contiene **34 tablas en total**: 33 para las entidades y operaciones del sistema y una tabla adicional de configuracion, `configuracion_correo_bitacora`. Las 34 forman parte de la base de datos. El conjunto tiene 260 campos, 64 claves foraneas y una vista. La vista `vw_estrellas_por_trabajo` cuenta favoritos activos y no es una tabla adicional.

Los PDF y anexos se guardan en la carpeta indicada por `Storage:Ruta`. PostgreSQL conserva sus referencias. Las fotos de perfil se guardan en `usuarios.foto_perfil` y forman parte del respaldo de la base.

Las estrellas representan usuarios que mantienen un trabajo en favoritos. Desactivar una marca conserva el registro sin contarlo. No hay puntuaciones ni promedios.

Biblioteca verifica el ejemplar y los permisos antes de publicar. La revision docente no publica automaticamente. Una version corregida conserva la anterior y necesita sus propias verificaciones y autorizaciones.

Los [diagramas y el diccionario](documentacion/) describen las relaciones, restricciones, indices, vista, funciones, procedimientos y triggers. Los SQL de `sql-init` definen el esquema utilizado por la API.

## 6. Correo

El envio utiliza el servidor SMTP configurado en el `.env` privado. Para Gmail se utiliza `smtp.gmail.com`, puerto `587` y `SMTP_TLS=true`. `SMTP_FROM` identifica el remitente y `SMTP_USER` la cuenta que se autentica. `SMTP_PASSWORD` contiene su contrasena de aplicacion. `SMTP_AVISOS` selecciona el buzon de avisos; vacio utiliza el remitente.

`correos_eventos` conserva los avisos pendientes y `correos_intentos` registra los intentos. Los reenvios autorizados conservan ese historial. Enviado significa aceptado por el servidor SMTP y no confirma lectura.

Los codigos de recuperacion duran diez minutos y se utilizan una sola vez. Su cola de envio es temporal; si un reinicio interrumpe el envio puede ser necesario solicitar otro codigo.

`configuracion_correo_bitacora` pertenece a la funcion de copias institucionales de bitacora por correo. Admite una unica fila: `unica` identifica esa configuracion, `destino` guarda la direccion que recibira las copias y `habilitado` permite activarlas o desactivarlas. Se crea con destino vacio y deshabilitada.

Esta configuracion esta en PostgreSQL porque `fn_encolar_copia_bitacora` y el trigger `trg_correo_copia_bitacora` la consultan directamente al generar la copia de un evento. Se puede cambiar mediante SQL sin editar archivos ni reiniciar la API para ese ajuste. No tiene un panel propio de administracion en la interfaz y no guarda contrasenas SMTP. Esas credenciales siguen en el `.env` privado.

El trigger diferido encola copias de eventos nuevos dentro de la misma transaccion. La API envia los mensajes de `correos_eventos` y registra los intentos. Deshabilitar esta configuracion impide generar nuevas copias; no cancela las que ya estan en cola. El envio tambien depende de que SMTP este configurado y habilitado en la API.

## 7. Base existente y respaldo

Conservar el nombre del proyecto Compose, volumenes y carpeta de archivos. Realizar un respaldo privado y pausar las escrituras antes de aplicar SQL pendientes en orden de numero y nombre. No ejecutar `down -v` ni repetir la creacion de tablas sobre una base con datos. Reiniciar Docker no aplica SQL nuevos a un volumen existente.

El [procedimiento de respaldo y restauracion](pruebas/RESPALDO.txt) explica como copiar la base y sus archivos durante la misma pausa. Las pruebas de restauracion se realizan en una copia aislada con `SMTP_HABILITADO=false`.

Los datos de `datos-prueba.sql` son opcionales y se cargan solo en una base de practica sin usuarios ni fichas. La [prueba guiada](pruebas/PRUEBA-GUIADA.txt) y la coleccion de Postman permiten comprobar la API.

## 8. Detener y continuar

Detener la API desde Visual Studio o con Ctrl C en su terminal. Para detener o iniciar PostgreSQL y pgAdmin sin borrar datos:

```powershell
docker compose -p scriptorium-archivum stop
docker compose -p scriptorium-archivum start
```

La API se vuelve a iniciar desde Visual Studio o con `dotnet run`. No publicar `.env`, claves, archivos de usuarios, fotos, sesiones ni respaldos.
