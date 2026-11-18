# Prueba guiada en Postman o en un cliente HTTP

Usa la base ficticia del README. Las rutas de aquí empiezan por `http://127.0.0.1:3000`. Cambia el puerto si lo personalizaste.

1. Inicia sesión como Biblioteca en `POST /api/auth/login` con JSON. Comprueba `GET /api/auth/me` y conserva la cookie en el cliente.
2. En `GET /api/admin/trabajos` copia el ID de Gestión de información. En `GET /api/admin/personas?q=Pe%C3%B1a` identifica el ID numérico de Ana María Peña de Prueba.
3. Abre `POST /api/admin/trabajos/{id}/pdf` con Body → form-data. La clave `archivo` es de tipo File. Carga `ejemplar-ficticio.pdf` y anota el ID que devuelve.
4. Abre el PDF en `GET /api/admin/trabajos/{id}/pdf?ejemplar={ejemplar_id}`. Guarda los controles con `PUT /api/admin/trabajos/{id}/controles` y el cuerpo mostrado abajo. Cambia los dos IDs por los que obtuvo tu copia.
5. Ejecuta `POST /api/admin/trabajos/{id}/publicar`. La ficha debe aparecer en el catálogo público y permitir lectura. Comprueba que `?operacion=descarga` devuelve `403`.
6. Sube otra copia del PDF como ejemplar B. El ID cambia. La vista previa con el ID de A queda en `404`. Publicar B con los controles de A devuelve `409`. Completa sus propios controles si quieres publicar B.
7. Prueba la consulta pública y los favoritos con la cuenta Lector. Con esa sesión, `GET /api/admin/trabajos` debe devolver `403`.
8. Con Biblioteca retira una ficha publicada mediante `POST /api/admin/trabajos/{id}/retirar` con `{"motivo":"Fin de la prueba"}`. Espera `404` en el detalle público y comprueba la actuación en `GET /api/admin/bitacora`.

```json
{
  "ejemplar_id": "REEMPLAZAR",
  "deposito_referencia": "UES-FICTICIO-01",
  "deposito_fecha": "2025-05-20",
  "deposito_verificado": true,
  "verificar": true,
  "conformidad_referencia": "Acta de prueba",
  "ficha_publica": true,
  "lectura": true,
  "descarga": false,
  "autorizacion": {
    "referencia": "Constancia ficticia",
    "otorgante_ids": [3],
    "fecha": "2025-05-20",
    "estado": "Vigente",
    "ficha": true,
    "lectura": true,
    "descarga": false
  },
  "decision": {
    "resultado": "Favorable",
    "motivo": "Revisión de ejemplo"
  }
}
```

Los permisos del ejemplar y los que concede la constancia son datos separados. Al probar otros niveles, modifica cada parte de forma coherente.
