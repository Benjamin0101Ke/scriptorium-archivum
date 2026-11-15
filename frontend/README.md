# Interfaz Angular de Benjamín Daniel

La guía completa está en [../README.md](../README.md).

Desde esta carpeta:

```powershell
npm.cmd ci
npm.cmd start -- --host 127.0.0.1 --port 4300
```

Abrí http://127.0.0.1:4300/. El modo local conserva las fichas y los PDF de prueba en ese navegador.

```powershell
npm.cmd test
npm.cmd run build
```

El build de producción desactiva la demostración y necesita la API real.

La API, PostgreSQL, Compose y el almacenamiento institucional corresponden a la integración con Roberto.
