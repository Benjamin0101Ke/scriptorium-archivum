# Contraste con los requisitos · 23 de noviembre de 2026

**Conclusión: existe cobertura funcional amplia y nuevas pruebas locales aprobadas, pero no corresponde declarar cumplimiento total del documento de especificación ni del servidor real.**

Se tomó como referencia la matriz y la especificación de la entrega final del 21 de noviembre, y se contrastaron con el código integrado y las [pruebas del 23 de noviembre](PRUEBAS-20261123.md). La matriz histórica tenía 38 elementos implementados, 13 parciales y seis diferidos; esa clasificación no se presenta como una nueva certificación.

## Criterios con evidencia nueva

| Requisito | Evidencia de esta revisión | Estado y alcance |
| --- | --- | --- |
| RF-01 a RF-03 · Consulta y catálogo | Búsquedas y filtros verificados, resultados públicos y pruebas de acceso. | Verificados en los recorridos locales ensayados. |
| RF-05, RF-06, RF-13, RF-15 · Ejemplar, lectura y control | PDF íntegro, respuestas por rango, permisos y versiones. | Verificados localmente; cada recurso conserva su autorización. |
| RF-08 y RF-09 · Corrección y retiro | Diez fichas corregidas y denegación tras retirar una publicación. | Verificados localmente. |
| RF-10 a RF-12 y RF-36 · Roles, autorización y decisión | Intentos de acceso denegados y publicación mediante controles completos. | Los recorridos probados pasan; no equivalen a una auditoría de seguridad exhaustiva. |
| RF-28 · Orden inicial | El catálogo vuelve a iniciar en Coincidencia; permite año, título y más guardados. | Coincidencia se conserva como valor inicial. Las estrellas son favoritos y deben describirse así en la especificación. |
| RNF-01 · Búsqueda en hasta 3 s en 95/100 consultas | 1.000 fichas, veinte sesiones y cien búsquedas; 100/100 aprobadas, P95 de 391 ms. | Aprobado para el escenario selectivo local documentado. Pendiente reproducir en el entorno operativo. |
| RNF-03 · Veinte intentos negativos sobre recursos | Solicitudes directas, permisos revocados y retiro; ningún PDF protegido entregado. | Aprobado localmente para el inventario registrado. |
| RNF-08 · Integridad de diez fichas | Varios autores, eñes y tildes; comparación de siete campos persistentes después de editar. | Diez de diez coincidencias locales. |
| RNF-09 · Adaptación de pantallas | 360, 390, 768 y 1280 px en Chrome y Edge. | Ocho recorridos aprobados; no representa todas las combinaciones de dispositivo y navegador. |

## Criterios que no se pueden cerrar con estas pruebas

| Requisito | Qué falta |
| --- | --- |
| RF-19 · Guía institucional vigente | Incorporar o confirmar el documento institucional aprobado. Una guía ficticia o un mensaje de ausencia no lo sustituye. |
| RF-27 · Enlace público estable | La ruta por identificador es estable, pero un dominio temporal de túnel puede cambiar. Hace falta una dirección pública permanente para acreditar permanencia del enlace compartido. |
| RNF-02 · Usabilidad | La prueba de tareas con cinco estudiantes reales, sin capacitación previa. |
| RNF-04 · Compatibilidad | Dos versiones de Chrome, Firefox y Edge. La evidencia actual cubre una versión instalada de Chrome y una de Edge. |
| RNF-05 · Disponibilidad | Treinta días de observación operativa. El servidor estaba apagado durante esta revisión. |
| RNF-06 · Respaldo semanal | La restauración técnica local pasó; falta comprobar la operación periódica en la instalación y la conservación de sus copias. |
| RNF-07 · Reinstalación externa | El arranque automatizado desde una base vacía es una comprobación técnica; no sustituye la ejecución por una persona externa en hasta cuatro horas. |

## Cambios de alcance que deben figurar en el documento

Son cambios solicitados para la aplicación, no errores que deban corregirse regresando al comportamiento anterior.

| Cambio | Consecuencia documental |
| --- | --- |
| Áreas temáticas opcionales | RF-04 y RF-07 no deben seguir exigiendo el área como uno de siete datos obligatorios sin registrar el cambio de criterio. Se mantiene la validación del resto de metadatos. |
| Favoritos en lugar de valoraciones manuales | Actualizar RF-A06, sus pantallas y criterios. El contador indica guardados; ya no debe describirse como una calificación introducida por el lector. |
| Estudiante, docente, administrador y superadministrador | Ampliar actores, matriz de permisos y casos de uso. La revisión docente no equivale por sí sola a una decisión del tribunal. |
| Proyectos de software por enlace HTTPS | Distinguir el ejemplar por enlace del PDF, sus validaciones y los permisos de difusión. |
| Cargas de 6 GB y complementos | Actualizar límites y protocolos de carga. El rendimiento de búsquedas no demuestra el de una transferencia de 6 GB. |
| Asignación docente, correos, recuperación y perfil académico | Registrar sus reglas, errores, autorizaciones y evidencia de aceptación. |
| Actualizaciones automáticas y animaciones | Documentar conservación de borradores, reintentos y reducción de movimiento, como mejoras de interacción. |

## Funciones diferidas

RF-17, RF-18, RF-21, RF-22, RF-23 y RF-25 pertenecen al alcance diferido de la matriz histórica. Tener tablas, correos o revisiones docentes de finalidad parecida no acredita automáticamente el flujo completo de propuesta, tribunal y versiones corregidas definido por esos requisitos.

La entrega final en Word/PDF, sus diagramas, presentación y matriz necesitan una actualización posterior para representar esta versión y sus cambios de alcance. Este contraste y el README no modifican silenciosamente aquellos documentos ni afirman una aprobación institucional que no se ha aportado.
