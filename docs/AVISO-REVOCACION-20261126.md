# Aviso explícito de revocación

Al revocar una constancia activa, el aviso anterior solo indicaba una actualización de controles. Ahora se registra `CONSTANCIA_REVOCADA` y se encola un mensaje con asunto «Constancia de difusión revocada», los efectos sobre los permisos y los pasos para consultar o corregir el envío. Se conservan los avisos de una decisión nueva cuando ambas acciones se guardan juntas.

El aviso usa los destinatarios del trabajo y la copia institucional configurada. Solo se genera al cambiar una constancia activa: guardar nuevamente la constancia ya revocada no duplica este evento. La operación y los avisos forman parte de la misma transacción.

## Validación

- Compilación de la API correcta.
- Prueba de integración contra una copia aislada de la base, sin trabajador SMTP: evento explícito, permisos deshabilitados, constancia revocada, correo a responsables, copia institucional, ausencia de duplicación y rollback sin cambios ni avisos. Siete comprobaciones correctas.
- Publicación comprobada con respuestas 200 de portada y API pública de carreras.

También se encontró un fallo de resolución DNS en el entorno de ejecución que impedía resolver Gmail. Se corrigió de forma persistente y se reintentaron doce avisos que habían agotado sus intentos. Al cerrar la comprobación, la cola tenía 236 avisos en estado Enviado y ninguno pendiente ni fallido. Esto confirma aceptación por SMTP, no lectura ni llegada a la bandeja Principal.

El ajuste de infraestructura y los respaldos se mantienen privados; este cambio no incorpora credenciales ni configuración del servidor al repositorio.
