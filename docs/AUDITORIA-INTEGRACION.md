# Auditoría funcional de Scriptorium Archivum

Pruebas iniciadas el 17/11/2026, 6:47:00 p. m., hora de El Salvador. Se verificó la instalación local en Windows, Angular y Chrome, con la API ASP.NET Core y PostgreSQL en Docker.

Pasaron 14 grupos de pruebas funcionales y las nueve pruebas de metadatos. Angular compiló correctamente. La primera prueba de sustitución se intentó después de retirar una ficha y recibió HTTP 409, el control esperado para una ficha retirada. La sustitución y el retiro se comprobaron después como recorridos separados.

Se dejaron estos dos documentos ficticios publicados en esta instalación para revisión manual. Sus identificadores corresponden a esta base de datos.

| ID | Documento | Acceso |
| --- | --- | --- |
| 10 | [[AUDITORÍA] Tesis de ejemplo · lectura y descarga](http://127.0.0.1:4301/trabajos/10) | Lectura y descarga del PDF y cuatro adjuntos |
| 11 | [[AUDITORÍA] Proyecto de ejemplo · solo lectura](http://127.0.0.1:4301/trabajos/11) | Lectura del PDF. Descarga por API rechazada con HTTP 403. |

Los adjuntos del primer documento son paquetes válidos de Word y Excel, un ZIP y un archivo JavaScript. El PDF principal de cada ejemplo contiene texto distinto para comprobar que el visor abre el ejemplar correcto.

| Adjunto | Bytes | SHA-256 del contenido descargado |
| --- | --- | --- |
| anexo-auditoria.docx | 932 | `5822d1fb0cb9ff722f7f5c5aa029b43c4a92b910ba672716b5a85c6328868a58` |
| datos-auditoria.xlsx | 1514 | `721f41ff9af03441d3bd1b53cbd4eebb805f522e84add6b151d79907e3b0c57e` |
| codigo-auditoria.zip | 346 | `63c52293a7b7a2f5f4bc4ba58499379d06c32df80a40f5fe836db768df62d21e` |
| ejemplo-auditoria.js | 38 | `caede2fc7aabd9e9eddd9ec044371172b623706a5a5b116c1e2853d4e6d0dc91` |

| Recorrido | Resultado |
| --- | --- |
| Registro desde HTML, rol Estudiante y sesión al recargar | Correcto |
| Dos PDF enviados desde Mis envíos y persistentes al recargar | Correcto |
| Word, Excel, ZIP y JavaScript adjuntados desde el envío propio | Correcto |
| Biblioteca revisa el PDF correcto y publica mediante el formulario | Correcto |
| Autorización de adjuntos y descarga pública con comprobación SHA-256 | Correcto |
| Catálogo, búsqueda por departamento, PDF dentro de la ficha y móvil | Correcto |
| Favoritos por cuenta, recarga y eliminación | Correcto |
| Nombre actualizado en Administración y foto persistente | Correcto |
| Accesos de cinco roles y rechazo de documentos inválidos | Correcto |
| Reemplazo A por B, ocultación pública y rechazo de controles antiguos | Correcto |
| Retiro público y bloqueo de modificación posterior | Correcto |
| Deshabilitación, revocación de sesiones y nuevo ingreso | Correcto |
| Sin errores de JavaScript en los recorridos de comprobación | Correcto |
| Persistencia de sesión, publicaciones, PDF y adjuntos tras reiniciar API y PostgreSQL | Correcto |

El inventario de PostgreSQL coincide con el almacenamiento físico. Se encontraron 20 referencias y 20 archivos, con cero faltantes, cero diferencias de tamaño y cero archivos sin referencia. Los SHA-256 de los dos PDF y los cuatro adjuntos descargados coinciden con los originales.

La consulta de integridad no encontró publicaciones sin elegibilidad para ficha pública ni restricciones de PostgreSQL pendientes de validación. Los intentos de usar un PDF inválido, subir un ejecutable, reutilizar controles de una versión anterior y publicar desde una cuenta sin permiso fueron rechazados.

| Componente | Comprobación |
| --- | --- |
| API | Respuesta HTTP 200 y recorridos de autenticación, consulta y gestión. |
| PostgreSQL | `pg_isready`, consultas de integridad y restauración del respaldo. |
| pgAdmin | Contenedor activo conectado al mismo proyecto y volumen persistente. |
| Mailpit | Contenedor activo y saludable. El SMTP activo de la API es Gmail. |
| Gmail | Conexión SMTP con TLS y autenticación aceptada por Google. Esta corrida no envió un correo a una bandeja real. |
| Sesión | Cookie HttpOnly y SameSite Strict. La sesión de Biblioteca sobrevivió al reinicio de API y PostgreSQL. |

Se corrigió la ubicación del almacenamiento activo. Docker ahora monta la carpeta de la instalación principal. Se copiaron los archivos anteriores y se comparó su contenido antes de cambiar el montaje.

```text
C:\Users\benja\Downloads\scriptorium-archivum\backend\storage\trabajos
```

PostgreSQL conserva sus datos en el volumen `scriptorium-integracion_postgres_data`. Las claves de sesión se conservan en `scriptorium-integracion_api_keys`. Después de reiniciar API y PostgreSQL, los dos ejemplos siguieron publicados y los PDF y adjuntos conservaron su contenido.

El respaldo final incluye la base completa en formato PostgreSQL y un ZIP con los 20 archivos. La base se restauró en una base temporal y se verificó la presencia de las dos publicaciones; la base temporal se eliminó al terminar.

```text
C:\Users\benja\Downloads\scriptorium-archivum\backend\backups\auditoria-final-20261117-185215.dump
C:\Users\benja\Downloads\scriptorium-archivum\backend\backups\auditoria-final-20261117-185215-archivos.zip
```

Se corrigió también la presentación del tamaño de los archivos pequeños, que aparecían como 0.0 KB. Ahora se muestran sus bytes.

Las pruebas utilizaron cuentas y fichas ficticias. Las dos fichas adicionales de control de retiro y sustitución quedaron retiradas y no aparecen en el catálogo. La ficha de control final tiene ID 13. El documento del usuario sigue disponible con su registro original.

Las capturas muestran el catálogo y el visor con el documento realmente renderizado.

- [Catálogo con documentos de auditoría](evidencia/auditoria-catalogo.png)
- [Documento abierto y adjuntos](evidencia/auditoria-visor.png)

El `.env`, los archivos subidos y los respaldos están excluidos de Git. La revisión del índice debe mantenerse antes del commit. Los ejemplos publicados viven en PostgreSQL; las capturas y este informe documentan la prueba en el repositorio.

Quedan por cargar las versiones oficiales del pénsum. La gestión y selección del plan existen, y el catálogo de versiones permanece vacío hasta registrar sus referencias oficiales.

El alcance de esta corrida fue funcional y local. La entrega del correo en una bandeja Gmail, las pruebas de carga concurrente y otros navegadores requieren comprobaciones específicas. El permiso de descarga se verificó en los endpoints y en los botones de la aplicación. El visor PDF nativo del navegador puede ofrecer guardar o imprimir el contenido recibido para lectura.
