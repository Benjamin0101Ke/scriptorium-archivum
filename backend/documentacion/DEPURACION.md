# Aplicar la depuración del esquema

La versión compatible de la API elimina los modelos, propiedades y relaciones retirados. El SQL `22_depurar_estructuras_sin_uso.sql` elimina cuatro tablas y nueve columnas del esquema `public`, sin usar `CASCADE`. Las tablas de fase futura y el historial docente se conservan. El inventario está en [USO-DEL-ESQUEMA.md](USO-DEL-ESQUEMA.md).

## Base existente

1. Preparar y compilar la API compatible antes de detener la versión anterior.
2. Detener las escrituras de la API y guardar un respaldo completo con `pg_dump -Fc`. Guardar también el código o la imagen anterior y los archivos de `storage/trabajos` en un destino privado. No agregar respaldos al repositorio.
3. Restaurar el respaldo en una base aislada con `pg_restore --exit-on-error`; probar allí el SQL 22 y la API con SMTP deshabilitado.
4. Aplicar únicamente el SQL 22 a la base existente mediante `psql -v ON_ERROR_STOP=1 -f ...`. No volver a ejecutar los scripts de creación ni eliminar volúmenes.
5. Iniciar la API compatible y comprobar catálogos, sesión, consulta de trabajos y recursos, así como los flujos de escritura en un entorno de prueba.

La migración usa una sola transacción y un tiempo máximo de espera de bloqueo de 10 segundos. Si encuentra una dependencia inesperada, falla sin usar eliminación en cascada. Puede repetirse si las estructuras ya no existen. Si se recrearon parcialmente después de migrar, una colisión del respaldo abortará para evitar sobrescribir el original.

## Conservación de valores y recuperación

`respaldo_depuracion.datos_20261128` contiene una fila por tabla retirada y una por grupo de columnas retiradas. Las tablas completas se guardan como arreglos JSON; las columnas se guardan junto con la clave primaria de su registro. El acceso del rol `PUBLIC` al esquema y a la tabla está revocado. La API no mapea ni consulta este respaldo.

Este resguardo permite inspeccionar valores y preparar una recuperación selectiva; **no sustituye al respaldo completo** de estructura, secuencias, permisos y datos. Para regresar a la versión anterior, detener la API, restaurar el respaldo completo en una base vacía y volver a desplegar el código o imagen anterior. No restaurar encima de la base activa ni descartar escrituras posteriores sin conciliarlas.

## Instalación nueva

El inicializador de PostgreSQL ejecuta todos los SQL ordenados y aplica la depuración al final. Los scripts anteriores son antecedentes de migración; el resultado vigente contiene 30 tablas públicas, 224 columnas de tablas, 55 claves foráneas y una vista. El esquema privado de respaldo se contabiliza por separado.

Los diagramas y el diccionario PDF aún describen el esquema previo. Deben sustituirse por los editables o exportaciones corregidos antes de presentar esos documentos como descripción del esquema depurado.

## Prueba reproducible

Con Docker Desktop iniciado, ejecutar desde la raíz del repositorio:

```powershell
./backend/pruebas/depuracion.ps1
```

El script crea y elimina exclusivamente un contenedor temporal, sin publicar puertos ni montar datos del proyecto. Comprueba la conservación de registros retirados y operativos, las reservas de fase futura, la repetición de la migración y la reversión completa si existe una vista dependiente inesperada.
