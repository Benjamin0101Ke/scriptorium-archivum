# Prueba destructiva únicamente sobre un contenedor temporal sin puertos ni volúmenes del proyecto.
$ErrorActionPreference = 'Stop'
$depuracionContenedor = 'scriptorium-prueba-depuracion-' + [guid]::NewGuid().ToString('N').Substring(0,8)
function DockerPrueba {
    & docker @args
    if ($LASTEXITCODE -ne 0) { throw 'Falló la operación de Docker de prueba.' }
}
try {
    DockerPrueba run -d --name $depuracionContenedor -e POSTGRES_PASSWORD=clave-solo-contenedor-temporal -e POSTGRES_DB=scriptorium_archivum postgres:17
    $listo = $false
    for ($i=0; $i -lt 30; $i++) {
        & docker exec $depuracionContenedor pg_isready -U postgres *> $null
        if ($LASTEXITCODE -eq 0) { $listo = $true; break }
        Start-Sleep -Seconds 1
    }
    if (!$listo) { throw 'PostgreSQL de prueba no inició.' }
    DockerPrueba cp (Join-Path $PSScriptRoot '../sql-init') "${depuracionContenedor}:/tmp/sql-init"
    DockerPrueba cp (Join-Path $PSScriptRoot 'depuracion-fixture.sql') "${depuracionContenedor}:/tmp/fixture.sql"
    DockerPrueba cp (Join-Path $PSScriptRoot 'depuracion-verificar.sql') "${depuracionContenedor}:/tmp/verificar.sql"
    DockerPrueba exec $depuracionContenedor bash -c 'set -e; for f in /tmp/sql-init/*.sql; do case "$f" in */22_*) break;; esac; psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f "$f" >/tmp/inicializacion.log 2>&1 || { cat /tmp/inicializacion.log; exit 1; }; done'
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/fixture.sql
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -c 'CREATE VIEW public.prueba_dependencia AS SELECT termino FROM public.palabras_clave_trabajo;'
    & docker exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/sql-init/22_depurar_estructuras_sin_uso.sql *> $null
    if ($LASTEXITCODE -eq 0) { throw 'La migración aceptó una dependencia inesperada.' }
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -c "DO 'BEGIN IF (SELECT count(*) FROM pg_tables WHERE schemaname=''public'') <> 34 OR to_regclass(''public.palabras_clave_trabajo'') IS NULL OR to_regclass(''respaldo_depuracion.datos_20261128'') IS NOT NULL THEN RAISE EXCEPTION ''No se revirtió la transacción''; END IF; END;'; DROP VIEW public.prueba_dependencia;"
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/sql-init/22_depurar_estructuras_sin_uso.sql
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/verificar.sql
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/sql-init/22_depurar_estructuras_sin_uso.sql
    DockerPrueba exec $depuracionContenedor psql -U postgres -d scriptorium_archivum -v ON_ERROR_STOP=1 -q -f /tmp/verificar.sql
    Write-Output 'Correcto: eliminación, conservación de datos, reservas, reversión ante dependencias e idempotencia.'
} finally {
    if ($depuracionContenedor -match '^scriptorium-prueba-depuracion-[a-f0-9]{8}$') {
        & docker rm -f $depuracionContenedor *> $null
    }
}
