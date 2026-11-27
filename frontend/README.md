# Interfaz de Scriptorium Archivum

La interfaz Angular permite consultar el catálogo, gestionar favoritos, enviar trabajos y acceder a los paneles según el rol de la cuenta. La API comprueba los permisos y guarda los cambios en PostgreSQL.

## Iniciar con la API local

Los servicios del backend deben estar activos. La [guía del backend](../backend/README.md) explica cómo iniciarlos.

Desde la carpeta `frontend`:

```powershell
npm ci
npm start -- --configuration integration --host 127.0.0.1 --port 4301
```

`npm ci` instala las dependencias indicadas en `package-lock.json`. `npm start` inicia Angular; la configuración `integration` utiliza la API real mediante el proxy de `proxy.conf.json`.

Abrir `http://127.0.0.1:4301/` y mantener la terminal abierta. Si cambia el puerto de la API, ajustar el destino del proxy y reiniciar Angular.

En PowerShell, si la ejecución de `npm.ps1` está bloqueada, usar `npm.cmd` en los mismos comandos.

## Demostración sin backend

```powershell
npm start -- --configuration demo --host localhost --port 4302
```

Este modo utiliza datos locales del navegador. Permite recorrer la interfaz, pero no comprueba la conexión, los permisos ni la persistencia de la API.

## Pruebas y compilación

```powershell
npm test
npm run build
```

`npm test` ejecuta las pruebas incluidas en el proyecto. `npm run build` genera la compilación de producción, que desactiva la demostración y necesita acceso a la API.

El [README principal](../README.md) describe las funciones, los roles y la estructura del sistema. El [contrato de integración](../backend/integracion/CONSUMIR-API.txt) detalla las rutas y el uso de la sesión.
