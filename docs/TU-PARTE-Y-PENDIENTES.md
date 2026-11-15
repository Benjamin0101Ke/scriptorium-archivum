# Alcance de la entrega de Benjamín Daniel

## Archivos propios

| Grupo de archivos | Trabajo de Benjamín | Continuación coordinada |
| --- | --- | --- |
| src/app/app.html y src/styles.css | Encabezado, menú, pie, identidad, formularios, distribución y diseño adaptable | Todo el equipo conserva los estilos comunes |
| pages/login.html y login.ts | Vista de ingreso y envío al servicio común | Roberto implementa autenticación y sesión en servidor |
| pages/catalogo.html | HTML de inicio, búsqueda, filtros, tarjetas y estados vacíos | Oscar desarrolla y prueba consulta y filtros contra la API |
| pages/ficha.html | HTML de ficha, recursos y enlaces | Oscar completa comportamiento público, con el contrato de Roberto |
| pages/favoritos.html | HTML de lista personal, botones y mensajes | Oscar prueba agregar, consultar, quitar y aislamiento de cuentas |
| pages/biblioteca.html | HTML de metadatos, ejemplar, evidencias, permisos, decisión, retiro y enlaces | Exequiel continúa las interacciones visuales acordadas |
| pages/biblioteca.ts | Preparación de conexión frontal del panel y mensajes de resultado | Benjamín Daniel integra con Roberto, Exequiel no desarrolla la API |
| core/api.ts | Cliente común, sesión y errores de red | Coordinar formatos y cookies con Roberto |
| core/metadata.ts | Validación de siete metadatos como apoyo a Roberto | Roberto debe repetir las validaciones en el servidor |
| app.routes.ts | Navegación y acceso a vistas | Las guardas del frontend no reemplazan permisos del servidor |
| core/demo.ts y core/demo/ | Demostración local con cuentas lectoras de prueba, fichas, favoritos y PDF persistentes en el navegador | Roberto implementa persistencia compartida, archivos y autenticación en el servidor |
| README.md, docs y CI | Guía de instalación, uso, revisión y trabajo en GitHub | Mantener documentación al integrar |

Los archivos TypeScript públicos son conectores y ejemplos iniciales para hacer navegables tus pantallas. Su presencia no significa que se haya completado ni reasignado la parte funcional de Oscar. El simulador únicamente permite revisar tu interfaz.

## Relación con los avances

| Interfaz preparada | Requisitos relacionados | Estado real |
| --- | --- | --- |
| Búsqueda, filtros, catálogo, ficha, orden | RF-01 a RF-04, RF-28 | Plantillas y vista demo; falta comprobar API y conjuntos esperados |
| Ejemplar, lectura y descarga | RF-05, RF-06, RF-13, RF-31 | Asociación por PDF o referencia. Visor y descarga locales cuando se habilitan. Pendiente integrar entrega controlada por servidor |
| Alta y corrección | RF-07, RF-08, RNF-08 | Validación por campo con foco en el primero inválido y guardado local. Pendiente cotejar validación y persistencia de API |
| Login y funciones personales | RF-10, RF-20 | Ingreso y registro de lectores de prueba. La v3 no especifica un formulario de autorregistro. Pendiente autenticación y autorización con API |
| Constancia, permisos y ejemplar final | RF-11, RF-12, RF-15 | Formularios y payload propuesto; falta evidencia y validación del backend |
| Depósito y decisión | RF-14, RF-30, RF-36 | Campos y responsables previstos; fecha/usuario fiables deben venir del servidor |
| Publicación y retiro | RF-09, RF-24 | Controles y mensajes simulados; falta transición transaccional en API |
| Favoritos y enlace estable | RF-26, RF-27, RF-34, RF-35 | Favoritos persistentes por cuenta en la demo. Oscar debe probar integración y persistencia entre navegadores |
| Advertencia de duplicados | RF-29 | Diálogo de confirmación y tratamiento del conflicto propuesto |
| Material complementario | RF-32, RF-33 | Formularios y visualización; falta validación/filtrado del servidor |
| Guía y reporte opcional | RF-19, RF-16 | Vista de guía disponible o ausente y vista de conteo. Falta la edición vigente y el cálculo del servidor |
| Adaptación visual | RNF-09 | CSS adaptable preparado; completar pruebas de tareas a 360, 390, 768 y 1280 px |
| Rendimiento, seguridad, respaldo y disponibilidad | RNF-01 a RNF-07 | No acreditados con esta entrega de frontend |

La demostración incluye un envío local a Biblioteca a petición de Benjamín. Una cuenta lectora adjunta el PDF y consulta sus propios envíos. Biblioteca conserva la verificación y la decisión de publicación. La recepción local no acredita el depósito institucional ni incorpora el flujo académico diferido de N-12 al alcance de producción.

## Lista de trabajo tuya

- [x] Preparar Angular y estructura de pantallas.
- [x] Incorporar el logo del equipo.
- [x] Diseñar navegación, inicio, catálogo, ficha, login, favoritos y Biblioteca.
- [x] Separar HTML de la lógica TypeScript.
- [x] Preparar estilos comunes y puntos de adaptación.
- [x] Validar siete metadatos desde la interfaz.
- [x] Preparar llamadas de integración del panel.
- [x] Preparar demostración identificada como ficticia.
- [x] Documentar uso local y publicación de código en GitHub.
- [x] Agregar almacenamiento local, creación de cuentas lectoras de prueba y visor y descarga locales.
- [x] Agregar pantallas de guía, referencia del ejemplar, confirmación de retiro y validación con foco por campo.
- [ ] BD-11 a BD-16 · Levantar Compose con Roberto, acordar el contrato, validar los siete metadatos en el servidor y comprobar publicación y archivos de punta a punta.
- [ ] Coordinar con Oscar su lógica de consulta y favoritos.
- [ ] Coordinar con Exequiel las interacciones del panel y evidencia de PO-02/PO-04.
- [ ] Probar cuentas y roles reales sin depender de las guardas visuales.
- [ ] Probar sustituir PDF y que las autorizaciones anteriores no habiliten el nuevo.
- [x] Comprobar el desbordamiento en los cuatro anchos de pantalla y el foco inicial con teclado en Chrome.
- [x] Guardar capturas de la misma ficha local en pantalla de computadora y teléfono.
- [ ] Completar la navegación con teclado y las pruebas de aceptación del sistema integrado en los navegadores acordados.
- [ ] BD-17 a BD-19 · Consolidar requisitos y modelos con el equipo, actualizar la bitácora y la matriz con evidencias, reunir el paquete y coordinar la defensa.

La bitácora XLSX original no se ha modificado.
