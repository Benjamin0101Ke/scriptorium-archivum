# Integración de código · 23 de noviembre de 2026

Este registro describe la integración del 23 de noviembre. Para el estado actual, consultar el [informe de Entity Framework](INTEGRACION-ENTITY-FRAMEWORK-20261126.md).

Origen: rama `frontend-benjamin-daniel`, commit `123f4787f05b4fd09682be44e8774d51132be690`. Incluye `main` hasta `226994cb6d34c1f2edee68b41c44623ff1e1e822`.

Se incorporaron el panel docente del equipo, sus correcciones visuales y de formularios, la revisión visible para estudiante y Biblioteca y los proyectos de software por enlace. Se conservaron las mejoras posteriores de la aplicación: perfiles académicos, superadministración, cargas de 6 GB, actualización automática, selectores con búsqueda, favoritos, iconos y animaciones de navegación.

Decisiones de compatibilidad:

- La migración 19 incorpora las revisiones históricas al modelo común sin borrarlas ni duplicarlas al repetirse.
- Se conservan los contratos usados por clientes anteriores y se comprueban asignación y versión antes de registrar una revisión.
- En esa revisión se conservó la tabla histórica de valoraciones por compatibilidad. La migración posterior `20_estrellas_y_vista.sql` la elimina; el contador actual de estrellas utiliza únicamente favoritos activos.
- La publicación sigue dependiendo de Biblioteca y de sus controles; la revisión docente no publica automáticamente.
- Se optimizó la búsqueda de candidatos y se comprobó la autorización de los resultados finales.

La copia preparada para compartir contiene frontend, API, migraciones, ejemplos públicos y documentación. Se excluyen configuración de despliegue privada, archivos `.env` reales, claves, respaldos y documentos de usuarios.

No se realizó publicación en GitHub ni despliegue en Debian durante esta integración. Las [pruebas](PRUEBAS-20261123.md) son locales y los [límites de requisitos](REVISION-REQUISITOS-20261123.md) quedan explícitos.
