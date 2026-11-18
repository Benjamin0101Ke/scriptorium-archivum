# PostgreSQL, pgAdmin y API de Scriptorium Archivum

Esta carpeta incluye la API, los scripts de PostgreSQL y Compose. Docker levanta PostgreSQL, la API, pgAdmin y Mailpit para el correo de prueba.

## 1. Crear la base y arrancar los servicios

Abrí Docker Desktop y esperá a que inicie. En PowerShell 7, desde la raíz del repositorio:

```powershell
Set-Location backend
if (!(Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
notepad .env
```

Completá `POSTGRES_PASSWORD` y `PGADMIN_DEFAULT_PASSWORD` con contraseñas locales distintas. Para los ejemplos se usa `PGADMIN_DEFAULT_EMAIL=admin@scriptorium.dev`. Dejá `API_PROJECT_PATH=` vacío para compilar la API de esta carpeta. Guardá el archivo y cerrá el editor. Si ya habías iniciado la misma instalación desde otra carpeta, usá su `.env` y esa misma carpeta para conservar el acceso a sus PDF.

```powershell
docker compose -p scriptorium-integracion up -d --build
docker compose -p scriptorium-integracion ps
docker compose -p scriptorium-integracion exec -T postgres pg_isready -h 127.0.0.1 -U postgres -d scriptorium_archivum
```

Compose crea `scriptorium_archivum` y ejecuta los scripts de `sql-init/` en orden al crear un volumen nuevo. Si `pg_isready` todavía indica que PostgreSQL no acepta conexiones, esperá unos segundos y repetí ese comando.

| Servicio | Dirección local |
| --- | --- |
| API | `http://127.0.0.1:3000` |
| PostgreSQL desde Windows | `127.0.0.1:5433` |
| pgAdmin | `http://127.0.0.1:5052/` |
| Mailpit | `http://127.0.0.1:8025/` |

Si un puerto del host está ocupado, cambiá el valor correspondiente en `.env`. Si cambiás `API_PORT`, ajustá también `frontend/proxy.conf.json`.

## 2. Cargar los datos ficticios

Hazlo una vez en la base de integración recién creada. Este script queda fuera de `sql-init` y se detiene si ya hay usuarios o fichas.

```powershell
$OutputEncoding = [Text.UTF8Encoding]::new($false)
Get-Content -LiteralPath .\datos-prueba.sql -Raw -Encoding utf8 |
  docker compose -p scriptorium-integracion exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d scriptorium_archivum
Invoke-RestMethod http://127.0.0.1:3000/api/carreras
```

| Rol | Correo ficticio | Contraseña de integración |
| --- | --- | --- |
| Biblioteca | biblioteca@scriptorium.test | Practica-Biblioteca-2026! |
| Lector | lector@scriptorium.test | Practica-Lector-2026! |

El catálogo público seguirá vacío hasta publicar una ficha. Biblioteca verá Gestión de información en Borrador y Archivo académico en En revisión. Prueba el flujo con `pruebas/ejemplar-ficticio.pdf`.

## 3. Registrar la conexión y ver las tablas en pgAdmin

1. Abrí [pgAdmin](http://127.0.0.1:5052/) e ingresá con `PGADMIN_DEFAULT_EMAIL` y `PGADMIN_DEFAULT_PASSWORD` de tu `.env`.
2. En el panel izquierdo, hacé clic derecho en **Servers → Register → Server…**.
3. En **General**, poné **Scriptorium Archivum** como nombre.
4. En **Connection**, completá estos campos y pulsá **Save**.

   | Campo | Valor |
   | --- | --- |
   | Host name/address | `postgres` |
   | Port | `5432` |
   | Maintenance database | `scriptorium_archivum` |
   | Username | `postgres` |
   | Password | Tu valor local de `POSTGRES_PASSWORD` |

5. Desplegá **Servers → Scriptorium Archivum → Databases → scriptorium_archivum → Schemas → public → Tables**. Hacé clic derecho en `trabajos` o `usuarios` y elegí **View/Edit Data → All Rows**. Con los datos ficticios, `trabajos` muestra las dos fichas iniciales.

## 4. Conectar el frontend

Abrí otra terminal en `backend/` y ejecutá:

```powershell
Set-Location ../frontend
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4301
```

Abrí [Angular en el puerto 4301](http://127.0.0.1:4301/). El navegador consulta rutas relativas `/api/...` y el proxy de Angular las reenvía a Docker. El [contrato de integración](integracion/INTEGRACION.md) explica la sesión y las rutas.

### Actualizar una base que ya existe

Desde la carpeta que usa esa instalación de Compose, aplicá las migraciones en orden antes de recrear la API:

```powershell
Get-Content -Raw -LiteralPath .\sql-init\04_perfil_usuarios.sql |
  docker compose -p scriptorium-integracion exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d scriptorium_archivum
Get-Content -Raw -LiteralPath .\sql-init\05_recuperacion_y_roles.sql |
  docker compose -p scriptorium-integracion exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d scriptorium_archivum
foreach ($migracion in @('06_fotos_y_complementos.sql','07_oferta_academica.sql','08_planes_estudio.sql','09_departamentos.sql','10_valoraciones.sql','11_tipos_trabajo.sql','12_avisos_y_docentes.sql')) {
    Get-Content -Raw -Encoding utf8 -LiteralPath (Join-Path '.\sql-init' $migracion) |
      docker compose -p scriptorium-integracion exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d scriptorium_archivum
    if ($LASTEXITCODE -ne 0) { throw "No se pudo aplicar $migracion" }
}
docker compose -p scriptorium-integracion up -d --build --no-deps api mailpit
```

Las migraciones agregan el perfil, los envíos, los roles y la recuperación en un volumen existente. Se pueden ejecutar de nuevo. Las claves de la cookie se conservan en el volumen `api_keys`. La primera actualización del perfil puede pedir que ingreses nuevamente.

### Carreras, planes y cuentas

Administración ofrece **Gestionar carreras y departamentos** para agregar o editar carreras, departamentos, áreas temáticas y versiones de planes de estudios. Un plan pertenece a una carrera y puede enlazar su documento oficial. La carga inicial incluye 41 carreras de la oferta de San Miguel y las carreras técnicas de Morazán. Los códigos FMO-* son identificadores internos y las nueve áreas organizan la selección. La fuente es la [oferta oficial de ingreso UES](https://eel.ues.edu.sv/ingreso/index/contenido/600). Las versiones de pénsum deben registrarse con los documentos oficiales correspondientes.

Al seleccionar carrera en el registro, la cuenta se crea como Estudiante. Una cuenta lectora también puede elegir su carrera y plan en Mi perfil para pasar a Estudiante. Estas selecciones se conservan en `usuarios.id_carrera` y `usuarios.id_plan`. Los demás roles se asignan desde Administración.

Las fotos JPG o PNG de hasta 2 MB se guardan en `usuarios.foto_perfil`. Administración muestra el nombre visible actualizado, filtra por rol y estado, habilita o deshabilita cuentas y consulta la actividad.

### Archivos complementarios

En Mis envíos y Biblioteca se pueden adjuntar PDF, Word, Excel, ZIP y archivos de texto o código de hasta 25 MB. Se guardan en `storage/trabajos/` y se registran en `archivos_complementarios`. Biblioteca habilita cada archivo. Su descarga pública requiere una ficha publicada, permiso de descarga y correspondencia con el ejemplar vigente. Los archivos se entregan como descargas.

### Recuperación de contraseña

En la app, abrí **Iniciar sesión → Olvidé mi contraseña** con una cuenta de prueba. El correo aparecerá en [Mailpit](http://127.0.0.1:8025/). Ingresá el código de seis dígitos y elegí una contraseña nueva. El código vence a los diez minutos y se usa una vez.

Para entregar correo real, configurá un proveedor SMTP en el `.env` local. Ejemplo de valores para un remitente de Gmail que tenga habilitada la contraseña de aplicación:

```dotenv
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_TLS=true
SMTP_FROM=remitente@gmail.com
SMTP_USER=remitente@gmail.com
SMTP_PASSWORD=CONTRASENA_DE_APLICACION
```

Usá una contraseña de aplicación del remitente, cuando Google la permita en esa cuenta. Conservá la clave fuera de GitHub y recreá el contenedor `api` después de cambiar el `.env`. Los registros con correos `.test` sirven para probar en Mailpit.

### Roles

Con una cuenta administradora, abrí **Roles** en el menú. Podés asignar Lector, Estudiante, Bibliotecario, Docente revisor o Administrador. Lector y Estudiante pueden enviar trabajos. Biblioteca conserva las revisiones de difusión y la publicación. Docente revisor queda disponible para asignación mientras se define su flujo académico.

En una base nueva, el responsable de PostgreSQL puede habilitar la primera cuenta administradora ya registrada desde su cliente SQL, usando el correo de esa cuenta:

```sql
UPDATE public.usuarios u
SET id_rol = r.id_rol, version_rol = u.version_rol + 1
FROM public.roles r
WHERE r.nombre = 'Administrador' AND r.activo
  AND u.estado = 'Habilitada' AND u.correo_institucional = 'correo-autorizado@example.test';
```

Para probar los endpoints, importá la [colección de Postman](pruebas/Scriptorium.postman_collection.json). Empezá con **Carreras · GET público**, que debe responder `200 OK`. Después probá **Iniciar sesión → Consultar sesión** con la misma colección.

## 5. Comprobar y conservar los datos

- [Prueba guiada de sesión, fichas, PDF y publicación](pruebas/PRUEBA-GUIADA.md)
- [Respaldo y restauración](pruebas/RESPALDO.md)
- [Revisión de la consigna de Roberto](documentacion/REVISION.md)

Detén los contenedores con `docker compose -p scriptorium-integracion stop` desde esta misma carpeta. Para retomarlos usa `docker compose -p scriptorium-integracion start`. Usa la misma carpeta y el mismo nombre del proyecto para recuperar el volumen y el almacenamiento que ya estabas usando.

Para compartir la instalación por GitHub, usá `.env.example` como plantilla. Git ignora `.env`, los PDF de usuarios en `storage/trabajos/` y los respaldos. Las contraseñas locales de PostgreSQL, pgAdmin y SMTP quedan en tu máquina.

### Valoraciones y paginación del catálogo

La migración 10 crea public.valoraciones. Cada cuenta autenticada guarda una valoración entera de 1 a 5 por trabajo publicado y puede actualizarla o quitarla. La clave primaria compuesta evita votos duplicados. La API valida el estado del trabajo y publica el promedio y el número de valoraciones.

El catálogo usa el orden Mejor valorados por defecto, con desempate por cantidad de valoraciones y título. Muestra hasta 12 tarjetas por página. Con 6, 9 o 12 resultados se muestran todos; con 13 se muestran 12 en la primera página y uno en la segunda. Los controles calculan el total y las tarjetas de otras páginas quedan fuera del DOM. La búsqueda consulta el listado de la API y pagina su presentación en el navegador.

El diseño inicial se conserva mientras el trabajo no tiene votos. Los colores por promedio son dorado para 5, plateado desde 4.5, bronce desde 4, turquesa desde 3, violeta desde 2 y coral por debajo de 2.

### Tipos de trabajo

En Administración → Gestionar catálogos → Tipos de trabajo se agregan, renombran o desactivan tipos de hasta 60 caracteres. La migración 11 prepara la columna activa y agrega Trabajo de grado, Tesis, Proyecto de investigación, Proyecto de software, Informe técnico y Monografía.

Los tipos activos aparecen en Mis envíos y Biblioteca. Desactivar un tipo conserva sus fichas existentes y permite corregirlas manteniendo ese tipo. El catálogo público conserva el filtro de los tipos que ya tienen trabajos publicados. El código fuente de una tesis se adjunta como archivo complementario.

## Correos automáticos y docentes

La [guía de avisos](../docs/AVISOS-CORREO.md) explica destinatarios, asignación de docentes, plantilla con logo, cola de reintentos y pruebas. Administración asigna un docente a cada estudiante y consulta el estado de entrega. La migración 12 agrega `usuarios.id_docente` y `correos_eventos`; no contiene credenciales.
