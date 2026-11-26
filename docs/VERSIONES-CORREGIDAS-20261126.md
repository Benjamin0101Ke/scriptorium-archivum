# Versiones corregidas — 26/11/2026

En Mis envíos, la cuenta que realizó un envío puede usar **Enviar versión corregida** si el trabajo está En revisión y el ejemplar actual tiene una decisión desfavorable de Biblioteca, una revisión docente Requiere cambios o una constancia revocada. Se muestran las observaciones para que la persona pueda atenderlas. La sustitución no se permite en trabajos publicados o retirados, ni sin una devolución vigente.

El formulario solicita el PDF corregido (hasta 6 GB, con la transferencia por partes ya existente) y una explicación de los cambios de 10 a 1500 caracteres. Para Proyecto de software solicita un enlace HTTPS sin credenciales. No crea otra ficha: añade un ejemplar con número de versión nuevo, manteniendo archivos, revisiones y autorizaciones anteriores.

La nueva versión permanece en revisión. No hereda la verificación del PDF, la constancia, la decisión ni los permisos del ejemplar previo. Revocar una constancia y solicitar correcciones son acciones diferentes: enviar otro archivo no restablece una autorización revocada. Biblioteca debe verificar y autorizar la versión nueva antes de publicarla.

El envío registra avisos para quien envió el trabajo, su docente asignado y Biblioteca. La copia institucional de bitácora también registra la acción si está habilitada en esa instalación. Un aviso registrado o encolado no implica confirmación de lectura del correo.

## API

- `GET /api/mis-envios/{id}/correccion`: versión actual, observaciones, revocación y disponibilidad de corrección. Solo la cuenta propietaria.
- `POST /api/mis-envios/{id}/reemplazo`: formulario con `ejemplar_id` actual, `cambios` y `archivo` o `enlace`. Admite `carga_id` para archivos por partes.
- Resultado `201`: versión nueva y estado En revisión.
- `401`: sin sesión; `404`: envío ajeno o inexistente; `409`: versión obsoleta o estado que no permite reemplazo; `400`: motivo, archivo o enlace inválidos.

La API vuelve a comprobar propietario, estado y ejemplar dentro de la transacción, con bloqueo de ficha. Dos solicitudes simultáneas sobre el mismo ejemplar no crean dos versiones corregidas. Un error de validación o una operación revertida no conserva un PDF temporal huérfano.

## Validación

- [27 comprobaciones de API](pruebas/correcciones-api-20261126.json) en una copia privada aislada de la base, con contenedor y almacenamiento de prueba: propiedad, sesión, rechazo de PDF inválido, conservación del SHA-256 del original, revocación, revisión docente, permisos nuevos deshabilitados, avisos y concurrencia.
- [6 comprobaciones de proyectos por enlace](pruebas/correcciones-enlaces-20261126.json), incluyendo rechazo de HTTP y de credenciales en la URL, y aceptación de HTTPS.
- [Prueba de interfaz](pruebas/correcciones-ui-20261126.json) con respuestas ficticias en Chrome a 390 y 1280 px: abrir formulario, elegir PDF, explicar cambios, enviar una vez y mostrar la confirmación; sin desbordamiento ni errores de JavaScript.
- Compilación .NET y Angular correctas; las 12 pruebas existentes del frontend pasan. Las advertencias previas de presupuesto CSS siguen sin impedir la compilación.
- No se modificaron trabajos reales para las pruebas. La instalación del servidor conserva sus fuentes existentes y recibe únicamente el controlador nuevo y el componente del formulario.
