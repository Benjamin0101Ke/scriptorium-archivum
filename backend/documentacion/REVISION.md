# Revisión de la consigna de Roberto

Este cotejo describe el modelo entregado originalmente. Las migraciones `04_perfil_usuarios.sql` y `05_recuperacion_y_roles.sql` amplían ese esquema con perfil, envíos, roles y recuperación. Los diagramas adjuntos documentan el diseño de esa entrega.

Se cotejaron las páginas 4 y 5 del reparto, la especificación v3, el DER, el relacional, el diccionario y la implementación. El modelo relacional y el SQL de creación coinciden en los nombres y tipos de las 184 columnas y en las 25 tablas. El DER representa el comprobante UES y el vínculo entre la ficha, el ejemplar, su constancia y la decisión. El diccionario describe el procedimiento de publicación y el trigger que se usan en PostgreSQL.

| Consigna | Resultado del cotejo |
| --- | --- |
| RC-01 y RC-02 · Arranque y PDF | Compose inicia PostgreSQL, la API y pgAdmin. El backend utiliza un volumen para PostgreSQL y una ruta relativa para los PDF. El equipo aún debe acordar cómo servir la web en el entorno compartido. |
| RC-03 y RC-04 · Esquema y DER | PK, FK, CHECK y restricciones únicas en SQL. Las dos FK compuestas vinculan la ficha con su ejemplar final y el recurso con la entrega de esa ficha. La constancia activa y el recurso principal tienen índices únicos parciales. Los documentos de diseño se incluyen. |
| RC-05 · Datos de prueba | Script de carga separado en una instalación sin fichas ni cuentas. Dos roles, dos fichas y un PDF ficticio incluidos. |
| RC-06 · Respaldo | Se restauraron en una instalación aislada diez fichas publicadas, su inventario de PDF, revisiones, constancias, bitácora y zona horaria. La aceptación requiere dos copias dentro de siete días y cotejo supervisado con Oscar. |
| RC-07 y RC-08 · Sesión y fichas | Implementados sesión con cookie, acceso por rol, transacciones de metadatos y aviso de duplicados. |
| RC-09 a RC-12 · Depósito, ejemplar, permisos, publicación y retiro | Implementados. El comprobante se verifica aparte del código interno. Constancia y decisión ligadas al ejemplar y permisos independientes. La publicación reevalúa nueve controles. Suspensión y retiro conservan bitácora. |
| RC-13 · Guía y enlaces | Rutas implementadas. La edición oficial de la guía se registra cuando la unidad responsable la proporcione. |
| RC-14 y RC-15 · Catálogo y entrega | Búsqueda con filtros y orden, consulta por URL estable y control de lectura o descarga en cada petición. Se probaron los cuatro niveles de permiso y 26 solicitudes negativas sin entregar bytes del PDF. Falta medir RNF-01 con la interfaz y la carga indicada en la especificación. |
| RC-16 · Favoritos y reporte | Favoritos de la cuenta y consulta de reporte anual implementados. El equipo confirma si incorpora el reporte opcional en la interfaz. |

El modelo conserva estructuras del trámite académico previstas para una fase futura. La publicación y la verificación del ejemplar de Biblioteca sí corresponden a la v3.

## Evidencia al preparar la entrega

- La copia extraída del ZIP arrancó sin reutilizar la configuración ni los datos locales de Roberto. PostgreSQL creó 25 tablas, tres funciones, tres procedimientos y un trigger, en UTF-8 y la zona America/El_Salvador.
- Se comprobaron diez rechazos en PostgreSQL. Incluyeron las dos FK compuestas, los CHECK de año, comprobante, retiro, verificación y decisión y la unicidad de favoritos, constancias activas y recurso principal.
- En una copia temporal del Angular del equipo se comprobó que el proxy local permite iniciar sesión, recuperar la sesión al recargar, abrir Biblioteca, consultar el catálogo y recibir un PDF. El equipo debe conectar la vista previa de Biblioteca en modo real y ensayar los formularios y el despliegue compartido.
- Sigue pendiente RNF-01 con 1 000 fichas, 20 sesiones concurrentes y 100 consultas desde la interfaz. La evidencia de un respaldo aislado permite repetir el procedimiento, pero la observación semanal y la validación conjunta tienen que registrarse por separado.
