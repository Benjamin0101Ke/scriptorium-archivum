# Interfaz Angular de Benjamín Daniel

La guía completa está en [../README.md](../README.md).

Desde esta carpeta:

```powershell
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4301
```

Abrí http://127.0.0.1:4301/ con la API y PostgreSQL activos. Para usar la demostración en el navegador, iniciá Angular con `npm.cmd start -- --configuration demo --host localhost --port 4302`.

```powershell
npm.cmd test
npm.cmd run build
```

El build de producción desactiva la demostración y necesita la API real.

La API, PostgreSQL, Compose y el almacenamiento institucional corresponden a la integración con Roberto.
