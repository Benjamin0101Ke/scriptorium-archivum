# Verificación local de la interfaz

El 14 de noviembre de 2026 se ejecutó la compilación de producción y pasaron las nueve pruebas de metadatos.

En Chrome, sobre un perfil de prueba limpio en `http://127.0.0.1:4300/`, se comprobó lo siguiente.

- Registro e inicio de sesión de un lector local, favoritos separados por cuenta y conservación de la sesión al recargar la pestaña.
- Alta de una cuenta lectora desde **Agregar tesis o proyecto**, rechazo de campos vacíos y PDF inválidos, conservación del envío tras recargar y separación de envíos entre cuentas. Biblioteca recuperó el PDF para revisión y el catálogo ocultó la ficha pendiente.
- Restricción del panel de Biblioteca a la cuenta de prueba de ese rol, navegación inicial con Tab y foco en el primer metadato inválido.
- Alta de ficha, consulta de la vista de guía y aviso al intentar publicar con controles pendientes.
- Carga, conservación tras recargar, apertura en el visor y descarga del PDF ficticio. La prueba cotejó el tamaño del archivo recuperado con el original.
- Publicación de la ficha, suspensión al reemplazar el ejemplar, asociación de referencia institucional y confirmación de retiro con motivo.
- Catálogo, panel de Biblioteca y ficha con visor sin desplazamiento horizontal de la página a 360, 390, 768 y 1280 px.

Capturas de la misma ficha ficticia en [pantalla de 390 px](evidencia/ficha-local-390.png) y [pantalla de 1280 px](evidencia/ficha-local-1280.png).

## Pendiente de verificar con el equipo

- Contrato e integración con la API de Roberto, cuentas reales y autorización del servidor por operación.
- Arranque limpio de Compose, persistencia compartida, respaldo, restauración y reinstalación.
- Pruebas de aceptación en los navegadores y versiones previstos, rendimiento con carga y prueba de tareas con estudiantes.

La bitácora y la matriz de cobertura pueden vincular estas capturas como evidencia de la demostración local.
