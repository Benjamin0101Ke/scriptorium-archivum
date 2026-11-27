# Contrato propuesto por la interfaz

Este documento conserva la propuesta inicial. Para integrar la versión actual, consultá el [contrato del backend](../backend/integracion/INTEGRACION.md).

Esta propuesta describe las rutas y los formatos previstos para la primera interfaz. No representa el contrato vigente ni acredita su implementación.

## Reglas comunes

- URL relativa /api, proxy local hacia http://localhost:3000.
- JSON para datos, FormData para carga del PDF.
- Sesión de servidor con cookie HttpOnly. Las llamadas incluyen credentials: same-origin.
- Respuestas fallidas con código HTTP apropiado y cuerpo: { "error": "Mensaje para el usuario" }.
- Campos adicionales de error admitidos: fields, duplicate y controles.
- Identificador estable de ficha y de ejemplar como cadenas.
- Operaciones administrativas restringidas por el servidor a Biblioteca habilitada.
- Responsables y fechas confiables los asigna la API. No confiar en el rol que muestra Angular.
- El límite de 25 MB del selector es una propuesta técnica de interfaz. El límite definitivo corresponde a la validación del servidor y al contrato vigente.
- Los nueve controles de RN-02 deben comprobarse en la API. El frontend solo los presenta.

## Sesión

| Método | Ruta sin /api | Cuerpo | Respuesta |
| --- | --- | --- | --- |
| POST | /auth/login | correo, password | id, nombre, correo, rol |
| GET | /auth/me | — | id, nombre, correo, rol o 401 |
| POST | /auth/logout | — | {ok:true} |

Roles que interpreta la interfaz: Biblioteca y Lector.

## Consulta

| Método | Ruta | Función |
| --- | --- | --- |
| GET | /trabajos | Lista pública, admite q, autor, anio, carrera, area, tipo, orden |
| GET | /trabajos/:id | Ficha pública y enlaces habilitados |
| GET | /trabajos/:id/pdf?operacion=lectura | PDF autorizado para visor |
| GET | /trabajos/:id/pdf?operacion=descarga | PDF autorizado para descarga |
| GET | /favoritos | Lista personal de fichas que siguen públicas |
| PUT | /favoritos/:id | Agregar sin duplicar |
| DELETE | /favoritos/:id | Quitar solo de la cuenta autenticada |

Valores de orden: coincidencia, anio, titulo.

Forma mínima de una ficha pública:

```json
{
  "id": "id-estable",
  "titulo": "Título del trabajo",
  "autores": ["Nombre Apellido"],
  "anio": 2026,
  "carrera": "Ingeniería de Sistemas Informáticos",
  "area": "Gestión documental",
  "tipo": "Trabajo de grado",
  "resumen": "Resumen",
  "lectura": false,
  "descarga": false,
  "enlaces": []
}
```

La lista pública no debe exponer constancias, responsables administrativos ni rutas físicas de archivos.

## Panel de Biblioteca

| Método | Ruta | Función |
| --- | --- | --- |
| GET | /admin/trabajos | Lista administrativa |
| GET | /admin/trabajos/:id | Detalle, ejemplar, autorización, decisión y controles |
| POST | /admin/trabajos | Guardar siete metadatos |
| PUT | /admin/trabajos/:id | Corregir siete metadatos |
| POST | /admin/trabajos/:id/pdf | FormData con campo archivo |
| POST | /admin/trabajos/:id/referencia | Cuerpo con referencia institucional verificable del ejemplar. Su integración con la operación de PDF depende del contrato del backend |
| PUT | /admin/trabajos/:id/controles | Registrar verificaciones, constancia, permisos y decisión |
| POST | /admin/trabajos/:id/publicar | Validar y publicar |
| POST | /admin/trabajos/:id/retirar | Cuerpo {motivo:"..."} |
| GET | /admin/trabajos/:id/enlaces | Lista administrativa de enlaces |
| POST | /admin/trabajos/:id/enlaces | url HTTPS, tipo y descripcion; inicialmente deshabilitado |
| PATCH | /admin/enlaces/:id | {habilitado:true} o false |
| GET | /admin/bitacora | Lista con id, nombre, fecha, evento |
| GET | /admin/reporte | Lista con anio, registradas, publicadas |
| GET | /admin/guia | disponible, mensaje y, cuando exista una edición vigente, titulo, referencia y url |

El endpoint de controles agrupa el envío de un formulario. No cambia la atomicidad de los requisitos del documento. Si el contrato separa depósito, verificación, autorización y decisión en endpoints independientes, saveControls() debe utilizar esas operaciones.

Alta y corrección envían los siete campos de metadatos. Para una advertencia de duplicado, se propone HTTP 409 con duplicate:true. La confirmación del usuario reenvía confirmarDuplicado:true.

La consulta administrativa agrega estos campos:

```json
{
  "estado": "En revision",
  "ejemplar_actual": "id-ejemplar",
  "deposito_referencia": "Referencia identificable",
  "deposito_fecha": "2026-11-14",
  "deposito_verificado": true,
  "ficha_publica": false,
  "lectura": false,
  "descarga": false,
  "ejemplar": {
    "id": "id-ejemplar",
    "nombre_original": "trabajo.pdf",
    "conformidad_referencia": "Referencia",
    "verificador": "id-cuenta",
    "fecha_verificacion": "2026-11-14T15:00:00Z"
  },
  "autorizacion": null,
  "decision": null,
  "controles": [{"ok": false, "label": "Condición pendiente"}]
}
```

Estados interpretados por la interfaz: Borrador, En revision, Publicado, Retirado. Acordar la representación definitiva.

Si se asocia una referencia institucional, el ejemplar usa `referencia_institucional` en lugar de `nombre_original`. El registro de cuentas lectoras y el almacenamiento del navegador pertenecen a la demostración local.

El cuerpo de controles contiene:

```json
{
  "ejemplar_id": "id-ejemplar",
  "deposito_referencia": "Referencia",
  "deposito_fecha": "2026-11-14",
  "deposito_verificado": true,
  "verificar": true,
  "conformidad_referencia": "Referencia",
  "ficha_publica": true,
  "lectura": true,
  "descarga": false,
  "autorizacion": {
    "referencia": "Constancia",
    "otorgantes": "Autores que autorizan",
    "fecha": "2026-11-14",
    "vence": "",
    "estado": "Vigente",
    "ficha": true,
    "lectura": true,
    "descarga": false
  },
  "decision": {
    "resultado": "Favorable",
    "motivo": "Motivo de Biblioteca"
  }
}
```

La fecha de depósito es opcional. La constancia admite Vigente o Revocada y vencimiento opcional. La decisión admite Favorable o Desfavorable.

## Verificaciones conjuntas pendientes

1. El servidor rechaza altas, correcciones, controles, cargas, publicaciones y retiros de un lector o visitante.
2. Los metadatos se validan de nuevo en la API.
3. Cada constancia y decisión corresponde al mismo ejemplar verificado.
4. La API detecta si cambió el ejemplar desde que se abrió el formulario.
5. Reemplazar un PDF deja pendiente el nuevo ejemplar.
6. Publicación comprueba las nueve condiciones con datos persistidos.
7. La entrega directa de PDF comprueba permiso por operación, estado y vigencia.
8. Retirar, revocar o vencer autorizaciones impide exponer contenido protegido.
9. La cuenta lectora solo recibe sus favoritos.
10. Confirmar rutas de producción, cookies, HTTPS y tratamiento de errores.
