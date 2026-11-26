# Copias institucionales y lista de Biblioteca — 25/11/2026

La migración `backend/sql-init/20_copia_interna_bitacora.sql` añade una copia por correo de cada evento nuevo registrado en `public.bitacora`. Incluye registros de cuentas, accesos, roles, habilitación, asignaciones, decisiones y acciones de trabajos cuando esas operaciones generan un evento de bitácora. No registra lecturas HTTP ni convierte cada fila de las tablas en un correo.

La copia identifica acción, responsable, fecha de El Salvador, registro afectado, detalle o motivo y destinatarios de los avisos asociados. Las decisiones incluyen el resultado y el motivo del aviso al estudiante. Es un comprobante del registro de la acción; la entrega y lectura del correo son cosas distintas. El estado de envío permanece en `correos_eventos` y sus intentos.

El trigger se ejecuta al cerrar la transacción: recoge los destinatarios insertados por la API y no envía mensajes de operaciones revertidas. La clave evento/destino evita duplicados. Si ya había un aviso pendiente para la misma dirección institucional, se enriquece ese aviso. No se reenvían eventos antiguos al instalar la migración ni se reenvían copias que ya figuran como enviadas.

Los códigos de recuperación y las contraseñas no se copian: la recuperación utiliza su transporte separado y no se incorpora a este resumen.

## Configuración privada

Después de aplicar la migración a una instalación existente, un administrador de la base de datos configura el buzón institucional:

```sql
UPDATE public.configuracion_correo_bitacora
SET destino='repositorio@example.org', habilitado=true
WHERE unica;
```

Sustituir la dirección de ejemplo por la propia. La migración no incluye el correo real ni secretos SMTP. Sin habilitarla no se generan copias adicionales. Para desactivar solo estas copias:

```sql
UPDATE public.configuracion_correo_bitacora SET habilitado=false WHERE unica;
```

El servicio de correos existente procesa las copias con los mismos reintentos que los demás avisos. No requiere un proceso adicional al arrancar.

## Biblioteca

Solo la lista de fichas tiene altura limitada y desplazamiento vertical. El encabezado, selector de estado, contador y acciones están fuera del área desplazable. La región tiene nombre accesible, foco de teclado y barra con espacio reservado. No se cambia la ficha abierta ni se descartan campos editados al desplazarse.

## Validación

- Prueba SQL: `backend/tests/copia-interna-bitacora.sql`, con destino ficticio y `ROLLBACK`. Comprueba ejecución diferida, motivo, responsable, destinatario, deduplicación, enriquecimiento de copia existente y desactivación.
- La migración se probó dentro de una transacción revertida antes de instalarla en el servidor; se guardó un respaldo privado previo.
- Las 12 pruebas de frontend y la compilación de producción completaron correctamente. Permanecen advertencias de presupuesto CSS, sin error de compilación.
- Prueba de diseño en Chrome a 1280 y 390 px: contenido mayor que el área visible y desplazamiento con rueda. La plantilla de prueba usa datos ficticios.
- Verificación autenticada en el sitio publicado: nueve fichas, área visible de 405 px, contenido de 1136 px y desplazamiento de 600 px; sin errores de JavaScript. La captura de esta revisión no se publica porque contiene datos de usuarios.
- El servidor conserva su versión previa de la aplicación; se aplican únicamente esta migración y el cambio de lista a sus fuentes existentes. La integración completa de `main` continúa siendo una operación diferente.
