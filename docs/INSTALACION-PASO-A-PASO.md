# Instalar y abrir Scriptorium Archivum en Windows

Esta guía usa PowerShell, Docker Desktop y Node.js con npm ya instalados. Docker ejecuta PostgreSQL, la API, pgAdmin y el buzón de pruebas. Angular se ejecuta en otra terminal.

## 1. Abrir Docker

1. Abrí **Docker Desktop** desde Inicio.
2. Esperá hasta que indique que el motor está funcionando.
3. Abrí Visual Studio Code y su menú **Terminal → New Terminal**.
4. Comprobá las herramientas, una línea a la vez:

```powershell
docker version
node --version
npm.cmd --version
```

Docker debe mostrar información de **Client** y **Server**. Si dice que no puede conectarse al motor, esperá a que Docker Desktop termine de iniciar.

## 2. Entrar a la carpeta del proyecto

En la computadora de Benjamin, la copia que contiene Git está aquí:

```powershell
Set-Location 'C:\Users\benja\Downloads\scriptorium-archivum-github'
Set-Location backend
```

En otra computadora, usá la ruta donde descargaste el repositorio. Para conseguir una copia nueva, desde una carpeta de trabajo:

```powershell
git clone --branch frontend-benjamin-daniel https://github.com/Benjamin0101Ke/scriptorium-archivum.git
Set-Location scriptorium-archivum/backend
```

**Elegí una sola carpeta para ejecutar el proyecto.** En la instalación actual de Benjamin, Docker ya usa `C:\Users\benja\Downloads\scriptorium-archivum\backend`. Para seguir trabajando con los datos y PDF actuales, entrá a esa carpeta. La copia terminada en `-github` se usa para preparar los cambios y subirlos. Iniciar desde otra carpeta puede usar una carpeta de archivos distinta aunque conserve la base.

## 3. Crear la configuración privada

Solo en una instalación nueva:

```powershell
if (!(Test-Path -LiteralPath '.env')) {
    Copy-Item -LiteralPath '.env.example' -Destination '.env'
}
notepad .env
```

En Bloc de notas:

1. En `POSTGRES_PASSWORD=`, escribí una contraseña larga y propia para PostgreSQL.
2. En `PGADMIN_DEFAULT_PASSWORD=`, escribí otra contraseña para entrar a pgAdmin.
3. Dejá `API_PROJECT_PATH=` vacío.
4. Para empezar con correo de pruebas, conservá `SMTP_HOST=mailpit` y los demás valores de ejemplo.
5. Guardá con **Ctrl+S** y cerrá Bloc de notas.

El archivo `.env` se queda en tu computadora. `.env.example` sí se comparte porque contiene campos vacíos y valores de ejemplo. No sobrescribas el `.env` de una instalación que ya funciona. Cambiar la contraseña en ese archivo no cambia automáticamente la contraseña de una base ya creada.

## 4. Encender los servicios

Desde `backend`, ejecutá:

```powershell
docker compose -p scriptorium-integracion up -d --build
docker compose -p scriptorium-integracion ps
```

La primera vez puede tardar varios minutos. Deben aparecer `postgres`, `api`, `pgadmin` y `mailpit` con estado **Up**. Después comprobá la base:

```powershell
docker compose -p scriptorium-integracion exec -T postgres pg_isready -U postgres -d scriptorium_archivum
```

Resultado esperado: **accepting connections**. Si todavía está iniciando, esperá unos segundos y repetí el comando.

Los SQL de `backend/sql-init` crean las tablas automáticamente al iniciar una base vacía. Si ya tenés una base anterior, seguí la sección **Actualizar una base que ya existe** del [README del backend](../backend/README.md); reiniciar Docker por sí solo no aplica las migraciones nuevas.

## 5. Datos ficticios, solamente para una base nueva y vacía

Si querés las cuentas y fichas de ejemplo:

```powershell
$OutputEncoding = [Text.UTF8Encoding]::new($false)
Get-Content -LiteralPath '.\datos-prueba.sql' -Raw -Encoding utf8 |
    docker compose -p scriptorium-integracion exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d scriptorium_archivum
```

Si el script indica que ya hay datos, detenete en este paso y continuá con el siguiente. No borres la base para volver a cargar los ejemplos. Las credenciales de ejemplo están en el README del backend; usalas únicamente para pruebas locales.

## 6. Abrir PostgreSQL con una interfaz

1. Abrí `http://127.0.0.1:5052/` en el navegador.
2. Ingresá con el correo `PGADMIN_DEFAULT_EMAIL` y la contraseña `PGADMIN_DEFAULT_PASSWORD` de tu `.env`.
3. Clic derecho en **Servers → Register → Server**.
4. En **General → Name**, escribí `Scriptorium`.
5. En **Connection**, completá:

| Campo | Qué escribir |
| --- | --- |
| Host name/address | `postgres` |
| Port | `5432` |
| Maintenance database | `scriptorium_archivum` |
| Username | `postgres` |
| Password | Tu contraseña `POSTGRES_PASSWORD` |

6. Pulsá **Save**.
7. Abrí **Servers → Scriptorium → Databases → scriptorium_archivum → Schemas → public → Tables**.
8. Clic derecho en `usuarios` → **View/Edit Data → All Rows**.
9. Para ver una cuenta recién creada, volvé a ejecutar la consulta o abrí de nuevo **All Rows**. La tabla abierta no se actualiza sola.

La contraseña para entrar a pgAdmin y la contraseña para conectar PostgreSQL son dos cosas distintas. El host `postgres` funciona dentro de Docker; un cliente instalado directamente en Windows usa `127.0.0.1` y puerto `5433` con la configuración de ejemplo.

## 7. Abrir la página

Abrí una segunda terminal. Para la instalación actual:

```powershell
Set-Location 'C:\Users\benja\Downloads\scriptorium-archivum\frontend'
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4301
```

En otra computadora, cambiá la ruta por la carpeta `frontend` de tu copia. Esperá el mensaje de compilación correcta y abrí `http://127.0.0.1:4301/`. Dejá esa terminal abierta. Si ya hay un servidor en ese puerto, usá el existente o detenelo con **Ctrl+C** antes de iniciar otro.

La configuración normal usa la API real mediante `/api` y una sesión por cookie. No uses la configuración `demo` para comprobar PostgreSQL.

## 8. Comprobar que guarda

1. Creá una cuenta ficticia desde la página, con un correo de prueba que no hayas usado.
2. Actualizá `usuarios` en pgAdmin y buscá ese correo en `correo_institucional`.
3. Cerrá sesión y volvé a ingresar con esa cuenta.
4. En **Mi perfil**, comprobá la carrera. **Mis envíos** usa esa carrera.
5. Enviá un PDF válido. Debe aparecer como **En revisión** y registrarse en `trabajos` y `recursos_digitales`.
6. Biblioteca revisa y guarda los controles antes de publicar. Solo las fichas publicadas aparecen en el catálogo.

Las contraseñas de usuarios se guardan como hash; no deben aparecer en texto legible. Los PDF y complementos están en `backend/storage/trabajos`; PostgreSQL conserva sus referencias. Respaldá tanto la base como esa carpeta, de forma privada.

## 9. Apagar y volver a iniciar

Detené Angular con **Ctrl+C**. Desde `backend`, para detener los contenedores conservando datos:

```powershell
docker compose -p scriptorium-integracion stop
```

Para volver a encenderlos:

```powershell
docker compose -p scriptorium-integracion up -d
```

No uses `down -v`: elimina los volúmenes donde están los datos de PostgreSQL.

## 10. Preparar GitHub sin datos privados

Desde la copia que tiene Git:

```powershell
Set-Location 'C:\Users\benja\Downloads\scriptorium-archivum-github'
git status
git add -- .gitignore README.md .github docs frontend backend
git diff --cached --check
git diff --cached --name-only
git ls-files -- backend/.env backend/storage backend/backups backend/respaldos
```

El último comando no debe mostrar ningún archivo. Revisá la lista del penúltimo: no debe contener contraseñas, respaldos, documentos subidos por usuarios ni claves privadas. Los SQL de estructura y migración sí se comparten; una exportación de tus datos reales no.

Cuando esté revisado:

```powershell
git commit -m "Corrige envios y fechas y documenta instalacion local"
git push origin frontend-benjamin-daniel
```

Esto sube la rama `frontend-benjamin-daniel`. Para verla en la página principal del repositorio, seleccioná esa rama; para integrarla en `main`, creá y revisá un pull request.

`.gitignore` evita agregar archivos nuevos, pero no elimina secretos subidos en commits anteriores. Si alguna clave real ya se publicó, hay que revocarla y reemplazarla; borrar el archivo en un commit nuevo no borra el historial.
