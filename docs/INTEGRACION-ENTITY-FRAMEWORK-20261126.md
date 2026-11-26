# Integración de Entity Framework y controles de constancia

Se integra la actualización `65c04ab` de R Reyes con las correcciones del flujo de constancias. El backend usa `AppDbContext` y Entity Framework Core, conservando consultas SQL parametrizadas para las funciones, procedimientos y consultas especializadas de PostgreSQL.

## Comportamiento conservado

- Una constancia nueva se guarda como Vigente con referencia, fecha y otorgantes registrados.
- Intentar revocar un ejemplar sin constancia previa se rechaza; la transacción no registra una decisión ni envía avisos de un guardado incompleto.
- La revocación conserva los datos de la constancia existente. Si se cambian referencia u otorgantes, se solicita guardar primero la constancia como Vigente.
- Los avisos del trabajo y la copia institucional incluyen los otorgantes, la referencia y el estado del ejemplar actual. Una versión nueva no hereda la constancia anterior.
- Cuando no existe constancia, el mensaje indica que está pendiente de registro y evita presentar referencias vacías como si hubieran sido guardadas.
- La interfaz deshabilita la revocación inicial hasta registrar una constancia y explica el paso requerido.
- Se mantienen las correcciones del propietario, el historial, los controles de concurrencia y el retorno a revisión sin heredar permisos.

## Actualización de la base existente

Con respaldo previo, revisar y aplicar los scripts pendientes en orden:

1. `backend/sql-init/20_estrellas_y_vista.sql` elimina la tabla de puntuaciones numéricas retirada `valoraciones` y crea `vw_estrellas_por_trabajo`, que cuenta favoritos activos.
2. `backend/sql-init/21_indice_favoritos.sql` añade el índice parcial de favoritos activos.

La tabla retirada no es la tabla `favoritos`. Se comprobó que los favoritos permanecen idénticos después de aplicar los scripts. Reiniciar los contenedores de una base existente no ejecuta automáticamente archivos SQL nuevos.

## Validación

- Compilación y publicación de la API correctas.
- Compilación de producción de Angular correcta; permanecen los avisos de presupuesto CSS de los componentes existentes.
- Diez comprobaciones de integración contra una copia aislada: persistencia de referencia y otorgantes, decisión desfavorable, revocación válida, rechazo sin constancia, recarga, correo, historial y mensajes sin constancia.
- Sesenta comprobaciones por HTTP, detalladas en [la evidencia de API](pruebas/integracion-ef-20261126.json): autenticación por cookie, roles, catálogo, filtros, PDF inválido, nuevos envíos, correcciones PDF/software, propiedad, versiones desactualizadas y concurrencia con una creación y un conflicto.
- Favoritos: agregar, repetir sin duplicación, consultar, retirar y actualizar el conteo de estrellas.
- PDF: lectura y descarga con respuesta parcial y firma válida.
- Tras actualizar la instalación, portada, catálogos, búsquedas y filtros respondieron 200. Se verificaron la vista y el índice; también la conservación de cantidades de trabajos, cuentas, entregas y recursos, además del contenido de favoritos.

Las pruebas de escritura usaron una base y almacenamiento aislados, cuentas de prueba y correo local de captura. Se retiraron el contenedor y la base de prueba al terminar. La configuración y los respaldos de la instalación permanecen privados.

Esta integración no certifica todos los requisitos del proyecto. Sigue pendiente la diferencia ya documentada entre el consumo frontend con `fetch` y el criterio de defensa que solicita `HttpClient`.
