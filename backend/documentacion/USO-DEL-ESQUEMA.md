# Uso del esquema de PostgreSQL

Revisión del código y los SQL del repositorio al 28/11/2026. Se distingue una operación implementada de una prueba ejecutada: disponer de un modelo de Entity Framework, o de registros en una tabla, no demuestra por sí solo su uso funcional. Este análisis no autoriza eliminar datos ni estructuras.

El esquema declara 34 tablas públicas (33 del sistema y la configuración técnica independiente del correo), 260 columnas, 64 claves foráneas, una vista, cinco funciones, tres procedimientos y dos triggers. Para evaluar funciones redefinidas debe leerse su última definición en el orden de los SQL.

## 1. Prioridad: estructuras sin consumidor actual

No se ha confirmado ninguna tabla que reúna **ambas** condiciones: ausencia de uso actual y ausencia de necesidad prevista. Los siguientes casos requieren confirmar el alcance antes de proponer una migración de eliminación. Sus claves foráneas y modelos no constituyen consumidores funcionales.

| Tabla | Finalidad del diseño y evidencia actual | Recomendación |
| --- | --- | --- |
| `palabras_clave_trabajo` | Términos asociados a trabajos. No se identificó lectura ni escritura funcional en la API; la búsqueda de `ConsultaPublica` utiliza título, resumen y áreas. | Confirmar si se requiere un vocabulario de palabras clave. El motivo histórico de su abandono no está documentado. |
| `permisos_trabajo` | Permisos individuales por usuario y trabajo. Las operaciones actuales de `Fichas`, `Ejemplares` y controladores autorizan mediante rol, propiedad y permisos de entrega/autorización. | Confirmar si se descarta el acceso individual; sus reglas no son equivalentes a los permisos actuales. |
| `historial_estados` | Historial de transiciones de solicitudes, entregas y trabajos. No se identificó escritura funcional. El trigger de publicación escribe en `bitacora`. | Evaluar si la bitácora cubre el historial requerido, incluidas futuras solicitudes. No asumir duplicación completa. |
| `evidencias_academicas` | Evidencias académicas del trabajo. La verificación actual de `Ejemplares` guarda referencia, responsable y fecha en `entregas`, sin consumir esta tabla. | Confirmar el alcance de evidencias y tribunal antes de decidir si sobra. |

La finalidad anterior se desprende del esquema. No existe evidencia suficiente para atribuir una decisión concreta a sus autores ni afirmar que nunca se utilizarán.

## 2. Módulos implementados con comprobación funcional pendiente

| Tabla | Consumidor real | Comprobación necesaria |
| --- | --- | --- |
| `procedimientos_deposito` | `Datos/Complementos.cs`, método `Guia`, consulta el procedimiento vigente por fecha para `/api/admin/guia`. | Probar en una base aislada un procedimiento vigente, uno futuro, uno vencido y la ausencia de documento; comprobar el enlace institucional aprobado. |
| `presencia_sesiones` | `Datos/Presencia.cs`: marca actividad, consulta conectados y elimina presencia al salir; rutas de presencia y administración. | Probar dos sesiones, actualización de actividad, expiración y cierre de sesión. Una tabla vacía no prueba desuso. |

`correos_intentos` y `recuperaciones_contrasena` también tienen consumidores implementados (ver tabla siguiente). La aceptación del SMTP externo debe comprobarse por separado y con autorización para enviar mensajes reales. Este documento no certifica el resultado de pruebas de entrega de correo.

## 3. Tablas reservadas para una fase futura

| Tabla | Finalidad y evidencia | Dependencias que conservar |
| --- | --- | --- |
| `solicitudes` | Flujo académico de solicitudes; `sql-init/02_crear_tablas.sql` la declara reservada. No representa el envío actual de PDF, que trabaja con `trabajos` y `entregas`. | Relaciones con usuarios, catálogos y entidades que referencian la solicitud. |
| `observaciones` | Observaciones estructuradas y respuestas, reservadas en el mismo SQL. Las observaciones docentes actuales se guardan como motivo de una `revision`. | Relaciones con solicitudes y responsables. |
| `distinciones_destacado` | Selección editorial con criterio, responsable y vigencia, reservada en el mismo SQL. Las estrellas actuales son favoritos, no distinciones. | Relaciones con trabajo y responsable. |

La columna `usuarios.proveedor_identidad` está reservada expresamente para integración de identidad (diccionario, página 45). No debe clasificarse como innecesaria por no participar en la autenticación actual.

## 4. Referencias a fase futura

En `sql-init/02_crear_tablas.sql`, el comentario inmediatamente anterior a `CREATE TABLE public.notificaciones` describía toda la tabla como futura. Se corrigió: `Datos/Avisos.cs`, método de registro de avisos, ejecuta `c.Notificaciones.Add(...)` para trabajos. La bandeja de lectura, el marcado de `fecha_lectura` y los avisos de solicitudes no tienen un flujo completo implementado; no debe presentarse todo ese módulo como terminado.

Las reservas de solicitudes y observaciones siguen siendo pertinentes en los editables DER (entidades `entity_solicitudes`, `entity_observaciones`), modelo relacional (`tbl_solicitudes`, `tbl_observaciones`) y diccionario (páginas 39 y 23). No requieren eliminar la indicación de fase futura. El diccionario, página 37, describe correctamente `revisiones_docentes` como histórico. Los documentos bajo `docs/historico/` describen un alcance histórico y no son la especificación vigente.

## 5. Tablas con operaciones actuales

Las rutas de archivos siguientes son relativas a `backend/APIScriptoriumArchivum`. Se indican consumidores funcionales, no solo entidades mapeadas.

| Tabla | Operación y dependencia real | Clasificación |
| --- | --- | --- |
| `usuarios` | `Datos/Usuarios.cs`, autenticación, perfil, roles y asignaciones; funciones de autorización y correo. | Activa |
| `personas` | `Usuarios`, `Fichas` y `Avisos`: identidad, autores y otorgantes. | Activa |
| `roles` | Autenticación, `GestionRoles` y `fn_es_biblioteca`. | Activa |
| `carreras` | `Catalogos`, perfiles y filtros; validación de oferta académica. | Activa |
| `departamentos` | Oferta académica y validación del departamento de la carrera. | Activa |
| `planes_estudio` | Oferta académica y validación de pertenencia del plan a la carrera. | Activa |
| `tipos_trabajo` | `Catalogos` y `Fichas`: metadatos y reglas por tipo. | Activa |
| `areas_tematicas` | Catálogos, metadatos y consulta pública. | Activa |
| `trabajos` | `Fichas`, consulta pública y procedimientos de publicación, retiro y suspensión. | Activa |
| `participaciones` | `Fichas` y `Avisos`: autores del trabajo y destinatarios; elegibilidad. | Activa |
| `trabajos_areas` | `Fichas` guarda áreas y la consulta pública filtra por ellas. | Activa |
| `entregas` | `Ejemplares`: versiones, verificación y selección del ejemplar final. | Activa |
| `recursos_digitales` | `Ejemplares` y elegibilidad: PDF, referencia UES, enlaces y disponibilidad. | Activa |
| `archivos_complementarios` | Controlador de archivos y `CargasArchivos`: carga, listado, habilitación y descarga. | Activa |
| `autorizaciones_difusion` | Controles de `Ejemplares`, revocación y función de elegibilidad. | Activa |
| `personas_autorizaciones` | Otorgantes de `Ejemplares`, elegibilidad y detalle en `Avisos`. | Activa |
| `revisiones` | Decisiones de Biblioteca y `RevisionesDocentes`; resultado por entrega y ámbito. | Activa |
| `favoritos` | Marcar/desmarcar y `vw_estrellas_por_trabajo`: cuenta solo favoritos activos. | Activa |
| `bitacora` | Registro de operaciones y trigger de cambio de publicación; genera copia institucional. | Activa |
| `configuracion_correo_bitacora` | Consumida indirectamente por las funciones de copia institucional: destino y habilitación. No contiene credenciales SMTP. | Activa indirecta |
| `correos_eventos` | `CorreoEventos` y funciones SQL: cola persistente, procesamiento y reenvío. | Activa |
| `correos_intentos` | `RegistroCorreos` y procesamiento de correo: historial de intentos y ciclos de envío. | Activa |
| `recuperaciones_contrasena` | `Recuperacion`: código protegido, vigencia, límite de intentos y confirmación. | Activa |
| `notificaciones` | `Avisos` inserta avisos de trabajos. No existe bandeja completa de lectura. | Escritura implementada; lectura pendiente |
| `revisiones_docentes` | `sql-init/19_integracion_repositorio.sql` importa su historial a `revisiones`; las revisiones nuevas usan esta última con ámbito docente. | Histórica / migración; conservar |

El bloque inicial del SQL 19 inserta desde `revisiones_docentes` y evita duplicar las revisiones importadas. Conservar el origen hasta validar la migración y definir una política de conservación.

## Dependencias indirectas de PostgreSQL

- `sp_publicar_trabajo` utiliza `fn_es_biblioteca` y `fn_elegibilidad_difusion`: roles, usuarios, trabajo, entrega seleccionada, recursos, autorizaciones, otorgantes y revisiones participan aunque no aparezcan en una consulta directa del controlador. La última definición de elegibilidad está en el SQL 19; no debe evaluarse únicamente la primera del SQL 03.
- `sp_retirar_trabajo` y `sp_suspender_difusion` validan al responsable y modifican el estado del trabajo.
- El cambio de estado dispara `trg_bitacora_estado_publicacion`, que ejecuta `fn_registrar_bitacora_estado` y escribe en `bitacora`, no en `historial_estados`.
- La inserción en bitácora activa el trigger diferido `trg_correo_copia_bitacora`, `fn_copiar_bitacora_por_correo` y `fn_encolar_copia_bitacora`. Consultan configuración, bitácora, usuarios, personas y trabajos para producir `correos_eventos`.
- `vw_estrellas_por_trabajo` es una vista sobre `favoritos` con `activo=true`; no es una tabla ni un sistema de puntuaciones de uno a cinco.

## Recomendaciones

Conservar las 34 tablas mientras se confirma el alcance de las cuatro candidatas. Completar las pruebas de módulos implementados con datos aislados y sin enviar correos reales. Si se aprueba retirar alguna estructura, preparar primero una migración conservadora, revisar claves foráneas y rutinas, respaldar los datos y probar restauración. No ejecutar scripts de creación desde cero sobre una base existente.
