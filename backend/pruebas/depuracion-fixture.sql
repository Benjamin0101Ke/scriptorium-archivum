-- Datos ficticios exclusivamente para el contenedor temporal de depuracion.ps1.
INSERT INTO personas(nombres,apellidos) VALUES ('Prueba','Depuración');
INSERT INTO usuarios(id_persona,correo_institucional,hash_contrasena,id_rol)
SELECT max(id_persona),'depuracion@example.test','hash-de-prueba',(SELECT min(id_rol) FROM roles) FROM personas;
INSERT INTO trabajos(codigo,id_tipo_trabajo,id_carrera,titulo,anio,resumen,objetivos,resultados,nivel_acceso,fecha_habilitacion)
SELECT 'PRUEBA-DEPURACION',(SELECT min(id_tipo_trabajo) FROM tipos_trabajo),(SELECT min(id_carrera) FROM carreras),'Prueba',2026,'Resumen','Objetivo conservado','Resultado conservado','Prueba',now();
INSERT INTO entregas(id_trabajo,numero_version,id_usuario_envia,fecha_envio,nota_cambios)
SELECT max(id_trabajo),1,(SELECT max(id_usuario) FROM usuarios),now(),'Nota conservada' FROM trabajos;
INSERT INTO palabras_clave_trabajo(id_trabajo,termino) SELECT max(id_trabajo),'Término conservado' FROM trabajos;
INSERT INTO permisos_trabajo(id_usuario,id_trabajo,nivel_permiso) SELECT max(id_usuario),(SELECT max(id_trabajo) FROM trabajos),'Prueba' FROM usuarios;
INSERT INTO historial_estados(id_trabajo,id_usuario_actor,estado_nuevo,ambito) SELECT max(id_trabajo),(SELECT max(id_usuario) FROM usuarios),'Borrador','Trabajo' FROM trabajos;
INSERT INTO evidencias_academicas(id_trabajo,id_usuario_registra,resultado,fecha_emision,emisor,referencia_respaldo) SELECT max(id_trabajo),(SELECT max(id_usuario) FROM usuarios),'Prueba',current_date,'Emisor','Referencia conservada' FROM trabajos;
