# Respaldo y restauración de prueba

El respaldo se realiza con las escrituras de la API detenidas para mantener la coherencia entre los datos y los PDF. La restauración se comprueba en una instancia aislada.

## Copia

En la carpeta que contiene Compose, usando el nombre de tu proyecto de Docker, crea un directorio de respaldo fuera del repositorio.

```powershell
$destino = 'C:\ruta\a\respaldo-script'
New-Item -ItemType Directory -Path $destino -Force
docker compose -p scriptorium-integracion stop api
try {
    docker compose -p scriptorium-integracion exec -T postgres pg_dump -U postgres -Fc -f /tmp/scriptorium.dump scriptorium_archivum
    if ($LASTEXITCODE -ne 0) { throw 'Falló pg_dump' }
    docker compose -p scriptorium-integracion cp postgres:/tmp/scriptorium.dump "$destino/scriptorium.dump"
    if ($LASTEXITCODE -ne 0) { throw 'Falló la copia del archivo de base' }
    if (!(Test-Path -LiteralPath .\storage\trabajos)) { throw 'Falta la carpeta de archivos de la API' }
    Copy-Item -LiteralPath .\storage\trabajos -Destination (Join-Path $destino 'trabajos') -Recurse -ErrorAction Stop
    docker compose -p scriptorium-integracion exec -T postgres rm -f /tmp/scriptorium.dump
}
finally {
    docker compose -p scriptorium-integracion start api
}
```

Registra la fecha, el conteo de fichas y el inventario de PDF. Guarda el respaldo en un lugar con acceso controlado.

## Restauración aislada

1. Prepara una copia descomprimida en otra carpeta con otros puertos, otro nombre Compose y su propio `.env`. En los comandos de ejemplo se llama `scriptorium-restore`.
2. Arranca solo PostgreSQL, espera a que esté listo y restaura en una base nueva llamada `scriptorium_restaurada`. El destino debe estar vacío.

```powershell
docker compose -p scriptorium-restore up -d postgres
docker compose -p scriptorium-restore exec -T postgres pg_isready -h 127.0.0.1 -U postgres
docker compose -p scriptorium-restore exec -T postgres createdb -U postgres scriptorium_restaurada
docker compose -p scriptorium-restore cp 'C:\ruta\a\respaldo-script\scriptorium.dump' postgres:/tmp/scriptorium.dump
docker compose -p scriptorium-restore exec -T postgres pg_restore -U postgres --exit-on-error --no-owner -d scriptorium_restaurada /tmp/scriptorium.dump
docker compose -p scriptorium-restore exec -T postgres psql -X -v ON_ERROR_STOP=1 -U postgres -d postgres -c "ALTER DATABASE scriptorium_restaurada SET TimeZone TO 'America/El_Salvador';"
```

Si `pg_isready` indica que PostgreSQL todavía no acepta conexiones, repite esa comprobación antes de ejecutar `createdb`. La comprobación usa TCP para esperar al servidor definitivo del contenedor.

3. Copia el directorio respaldado `trabajos` a `storage/trabajos` de esa otra carpeta. Comprueba que los PDF queden directamente en `trabajos`. En el Compose de **esa copia**, cambia `Database=scriptorium_archivum` por `Database=scriptorium_restaurada` en la cadena de la API.
4. Arranca la API con `docker compose -p scriptorium-restore up -d --build api`. Verifica una ficha y su PDF, el conteo, controles, permisos y bitácora. Compara los archivos con el inventario. La aceptación de RNF-06 requiere dos copias consecutivas separadas por hasta siete días y el cotejo de diez fichas con sus archivos y controles.
