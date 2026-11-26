using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Ejemplares
    {
        private NpgsqlDataSource baseDeDatos;
        private ArchivoPdf archivos;
        private Avisos avisos;
        private static string[] Controles =
        {
            "Metadatos completos", "Comprobante UES verificado", "Ejemplar final verificado",
            "Constancia vigente para el ejemplar", "Decisión favorable de Biblioteca",
            "Ficha pública autorizada", "Permisos de PDF respaldados", "PDF disponible y controlado",
            "Cuenta de Biblioteca y estado válido"
        };

        public Ejemplares(NpgsqlDataSource baseDeDatos, ArchivoPdf archivos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.archivos = archivos;
            this.avisos = avisos;
        }

        public async Task<JsonObject> Detalle(int id, CancellationToken ct)
        {
            const string detalle = """
                'estado', replace(t.estado_publicacion,'En revisión','En revision'),
                'ejemplar_actual', e.id_entrega::text, 'deposito_referencia', t.referencia_deposito_ues,
                'deposito_fecha', e.fecha_deposito, 'deposito_verificado', t.deposito_verificado_en IS NOT NULL,
                'ficha_publica', coalesce(e.difundir_ficha_habilitado,false),
                'lectura', coalesce(e.leer_pdf_habilitado,false), 'descarga', coalesce(e.descargar_pdf_habilitado,false),
                'ejemplar', CASE WHEN e.id_entrega IS NOT NULL THEN jsonb_build_object(
                    'id',e.id_entrega::text,'nombre_original', CASE WHEN re.clase='PDF' THEN re.nombre END,
                    'referencia_institucional',CASE WHEN re.clase='REF_UES' THEN re.ubicacion END,
                    'enlace_proyecto', CASE WHEN re.clase='ENLACE' THEN re.ubicacion END,
                    'conformidad_referencia',e.referencia_conformidad,'verificador',e.verificado_por::text,
                    'fecha_verificacion',e.verificado_en) END,
                'decision', CASE WHEN v.id_revision IS NOT NULL THEN jsonb_build_object('resultado',v.resultado,'motivo',v.motivo_decision,
                    'fecha',v.fecha_cierre,'responsable',v.id_usuario_revisor::text) END,
                'revision_docente', CASE WHEN d.id_revision IS NOT NULL THEN jsonb_build_object(
                    'resultado', d.resultado,
                    'observaciones', d.motivo_decision,
                    'fecha', d.fecha_cierre,
                    'docente', coalesce(du.nombre_mostrar, concat_ws(' ', dp.nombres, dp.apellidos)),
                    'version', e.numero_version) END,
                'historial_docente', COALESCE((
                    SELECT jsonb_agg(jsonb_build_object(
                        'resultado', x.resultado,
                        'observaciones', x.motivo_decision,
                        'fecha', x.fecha_cierre,
                        'docente', coalesce(ru.nombre_mostrar, concat_ws(' ', rp.nombres, rp.apellidos)),
                        'version', xe.numero_version) ORDER BY x.fecha_cierre, x.id_revision)
                    FROM public.revisiones x
                    JOIN public.entregas xe ON xe.id_entrega = x.id_entrega
                    LEFT JOIN public.usuarios ru ON ru.id_usuario = x.id_usuario_revisor
                    LEFT JOIN public.personas rp ON rp.id_persona = ru.id_persona
                    WHERE x.id_entrega IN (SELECT id_entrega FROM public.entregas WHERE id_trabajo = t.id_trabajo)
                      AND x.ambito_revision = 'Revisión docente' AND x.fecha_cierre IS NOT NULL
                ), '[]'::jsonb),
                'autorizacion', co.datos
                """;
            const string joins = """
                LEFT JOIN LATERAL (SELECT * FROM public.entregas x WHERE x.id_trabajo=t.id_trabajo ORDER BY x.numero_version DESC LIMIT 1) e ON true
                LEFT JOIN public.recursos_digitales re ON re.id_entrega=e.id_entrega AND re.clase IN ('PDF','REF_UES','ENLACE')
                LEFT JOIN LATERAL (SELECT * FROM public.revisiones x WHERE x.id_entrega=e.id_entrega AND x.ambito_revision='Difusión'
                    AND x.fecha_cierre IS NOT NULL ORDER BY x.fecha_cierre DESC,x.id_revision DESC LIMIT 1) v ON true
                LEFT JOIN LATERAL (SELECT * FROM public.revisiones x WHERE x.id_entrega=e.id_entrega AND x.ambito_revision='Revisión docente'
                    AND x.fecha_cierre IS NOT NULL ORDER BY x.fecha_cierre DESC,x.id_revision DESC LIMIT 1) d ON true
                LEFT JOIN public.usuarios du ON du.id_usuario = d.id_usuario_revisor
                LEFT JOIN public.personas dp ON dp.id_persona = du.id_persona
                LEFT JOIN LATERAL (SELECT jsonb_build_object('referencia', x.referencia_respaldo,
                    'fecha',(x.fecha_otorgamiento AT TIME ZONE 'America/El_Salvador')::date, 'vence',x.vigencia_hasta,
                    'estado', CASE WHEN x.estado='Activa' THEN 'Vigente' ELSE x.estado END,
                    'ficha',x.difundir_ficha_concedido,'lectura',x.leer_pdf_concedido,'descarga',x.descargar_pdf_concedido,
                    'otorgante_ids',array(SELECT pa.id_persona FROM public.personas_autorizaciones pa
                         WHERE pa.id_autorizacion=x.id_autorizacion ORDER BY pa.id_persona),
                    'otorgantes', (SELECT string_agg(concat_ws(' ',pe.nombres,pe.apellidos), '; ' ORDER BY pe.id_persona)
                         FROM public.personas_autorizaciones pa JOIN public.personas pe USING(id_persona)
                         WHERE pa.id_autorizacion=x.id_autorizacion)) datos
                    FROM public.autorizaciones_difusion x WHERE x.id_entrega=e.id_entrega
                    ORDER BY (x.estado='Activa') DESC,x.id_autorizacion DESC LIMIT 1) co ON true
                WHERE t.id_trabajo=@id
                """;
            string sql = "SELECT jsonb_build_object(" + Fichas.DatosBasicos + ", " + detalle + ")::text " +
                Fichas.RelacionesBasicas + " " + joins;
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            string? datos = await Comandos.Valor<string>(conexion, null, sql, ct, ("id", id));
            if (datos is null)
            {
                throw new ErrorApi(404, "La ficha no existe.");
            }
            JsonObject respuesta = JsonNode.Parse(datos)!.AsObject();
            string[] bloqueos = await Evaluar(conexion, null, id, ct);
            if (respuesta["estado"]?.GetValue<string>() is "Publicado" or "Retirado")
            {
                bloqueos = bloqueos.Append("09_ESTADO_ORIGEN_INVALIDO").ToArray();
            }
            respuesta["controles"] = JsonSerializer.SerializeToNode(ResumenControles(bloqueos));
            return respuesta;
        }

        public async Task<int> Registrar(int id, int actor, string clase, string ubicacion, string? nombre, long? bytes, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            await Suspender(conexion, tr, id, actor, "Sustitución del ejemplar.", ct);
            await Comandos.Ejecutar(conexion, tr, """
                UPDATE public.trabajos SET id_entrega_final=NULL, deposito_verificado_por=NULL, deposito_verificado_en=NULL WHERE id_trabajo=@id;
                UPDATE public.entregas SET estado='Sustituido' WHERE id_trabajo=@id AND estado <> 'Sustituido';
                """, ct, ("id", id));
            int ejemplar = await AgregarVersion(conexion, tr, id, actor, clase, ubicacion, nombre, bytes, ct);
            await avisos.Trabajo(conexion, tr, id, actor, "EJEMPLAR_ACTUALIZADO", "Ejemplar del trabajo actualizado",
                "Biblioteca asoció un nuevo ejemplar. Este ejemplar requiere sus propias verificaciones, autorización y decisión.", false, ct);
            await tr.CommitAsync(CancellationToken.None);
            return ejemplar;
        }

        internal static async Task<int> AgregarVersion(NpgsqlConnection conexion, NpgsqlTransaction tr, int id, int actor,
            string clase, string ubicacion, string? nombre, long? bytes, CancellationToken ct)
        {
            int ejemplar = await Comandos.Valor<int>(conexion, tr, """
                INSERT INTO public.entregas(id_trabajo,numero_version,id_usuario_envia)
                VALUES (@id,(SELECT coalesce(max(numero_version),0)+1 FROM public.entregas WHERE id_trabajo=@id),@actor)
                RETURNING id_entrega
                """, ct, ("id", id), ("actor", actor));
            await Comandos.Ejecutar(conexion, tr, """
    INSERT INTO public.recursos_digitales(id_trabajo,id_entrega,clase,ubicacion,nombre,tamano_bytes,tipo_formato,estado_carga,descripcion)
    VALUES (@id,@ejemplar,@clase,@ubicacion,@nombre::text,@bytes::bigint,
        CASE WHEN @clase='PDF' THEN 'application/pdf' WHEN @clase='ENLACE' THEN 'text/html' END,
        CASE WHEN @clase='PDF' THEN 'Disponible' END,
        CASE WHEN @clase='ENLACE' THEN 'Repositorio externo del proyecto de software' END)
    """, ct, ("id", id), ("ejemplar", ejemplar), ("clase", clase), ("ubicacion", ubicacion),
    ("nombre", nombre), ("bytes", bytes));
            return ejemplar;
        }

        public async Task GuardarControles(int id, int actor, ControlesFicha datos, CancellationToken ct)
        {
            if (!int.TryParse(datos.EjemplarId, out int ejemplar))
            {
                throw new ErrorApi(400, "Indica el ejemplar.");
            }

            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            int actual = await Comandos.Valor<int>(conexion, tr,
                "SELECT id_entrega FROM public.entregas WHERE id_trabajo=@id ORDER BY numero_version DESC LIMIT 1", ct, ("id", id));
            if (ejemplar != actual || actual == 0)
            {
                throw new ErrorApi(409, "El ejemplar cambio. Actualiza la ficha y vuelve a revisar los controles.");
            }

            await GuardarDeposito(conexion, tr, id, actor, ejemplar, datos, ct);
            if (datos.Verificar)
            {
                string? conformidad = Fichas.TextoOpcional(datos.ConformidadReferencia);
                if (conformidad is null)
                {
                    throw new ErrorApi(400, "Indica la referencia de conformidad académica.");
                }
                string? previa = await Comandos.Valor<string>(conexion, tr,
                    "SELECT referencia_conformidad FROM public.entregas WHERE id_entrega=@e", ct, ("e", ejemplar));
                if (previa is not null && previa != conformidad)
                {
                    await Suspender(conexion, tr, id, actor, "Cambio de la referencia de conformidad.", ct);
                }
                await Comandos.Ejecutar(conexion, tr, """
                    UPDATE public.entregas SET estado='Verificado', referencia_conformidad=@ref,
                        verificado_por=@actor, verificado_en=clock_timestamp() WHERE id_entrega=@e;
                    UPDATE public.trabajos SET id_entrega_final=@e WHERE id_trabajo=@id;
                    """, ct, ("ref", conformidad), ("actor", actor), ("e", ejemplar), ("id", id));
            }

            if (datos.Autorizacion is not null || datos.Decision is not null ||
                datos.FichaPublica == true || datos.Lectura == true || datos.Descarga == true)
            {
                bool verificado = await Comandos.Valor<bool>(conexion, tr,
                    "SELECT EXISTS(SELECT 1 FROM public.entregas WHERE id_entrega=@e AND estado='Verificado')", ct, ("e", ejemplar));
                if (!verificado)
                {
                    throw new ErrorApi(400, "Verifica el ejemplar antes de registrar autorización, decisión o permisos.");
                }
            }

            bool constanciaRevocada = datos.Autorizacion?.Estado == "Revocada" &&
                await Comandos.Valor<bool>(conexion, tr,
                    "SELECT EXISTS(SELECT 1 FROM public.autorizaciones_difusion WHERE id_entrega=@e AND estado='Activa')",
                    ct, ("e", ejemplar));
            if (datos.Autorizacion is not null)
            {
                await GuardarConstancia(conexion, tr, id, actor, ejemplar, datos.Autorizacion, ct);
            }
            await GuardarPermisos(conexion, tr, ejemplar, datos, ct);
            bool nuevaDecision = false;
            if (datos.Decision is not null)
            {
                string? razon = Fichas.TextoOpcional(datos.Decision.Motivo);
                if (datos.Decision.Resultado is not ("Favorable" or "Desfavorable") || razon is null)
                {
                    throw new ErrorApi(400, "Indica el resultado y el motivo de la decisión de Biblioteca.");
                }
                bool repetida = await Comandos.Valor<bool>(conexion, tr, """
                    SELECT coalesce((SELECT r.resultado=@resultado AND r.motivo_decision=@motivo FROM public.revisiones r
                      WHERE r.id_entrega=@e AND r.ambito_revision='Difusión' AND r.fecha_cierre IS NOT NULL
                      ORDER BY r.fecha_cierre DESC,r.id_revision DESC LIMIT 1),false)
                    """, ct, ("resultado", datos.Decision.Resultado), ("motivo", razon), ("e", ejemplar));
                if (!repetida || datos.RegistrarDecision)
                {
                    nuevaDecision = true;
                    await Comandos.Ejecutar(conexion, tr, """
                        INSERT INTO public.revisiones(id_entrega,id_usuario_revisor,fecha_inicio,fecha_cierre,ambito_revision,resultado,motivo_decision)
                        SELECT @e,@actor,ahora,ahora,'Difusión',@resultado,@motivo FROM (SELECT clock_timestamp() ahora) h
                        """, ct, ("e", ejemplar), ("actor", actor), ("resultado", datos.Decision.Resultado), ("motivo", razon));
                }
            }

            if ((await Evaluar(conexion, tr, id, ct)).Length > 0)
            {
                await Suspender(conexion, tr, id, actor, "Los controles de difusión dejaron de cumplirse.", ct);
            }
            if (constanciaRevocada)
            {
                string motivo = datos.Decision?.Motivo is { Length: > 0 }
                    ? $"\nMotivo de la decisión de Biblioteca: {datos.Decision.Motivo}"
                    : "\nConsulta con Biblioteca el motivo y los pasos que correspondan.";
                await avisos.Trabajo(conexion, tr, id, actor, "CONSTANCIA_REVOCADA",
                    "Constancia de difusión revocada",
                    $"Biblioteca revocó la constancia de difusión del ejemplar {ejemplar}." + motivo +
                    "\nSe deshabilitaron la ficha pública, la lectura y la descarga de este ejemplar." +
                    "\nSi tu trabajo está En revisión, puedes consultar Mis envíos y enviar una versión corregida cuando corresponda." +
                    "\nLa nueva versión necesita revisión y autorización de Biblioteca; enviarla no restablece la constancia revocada.", false, ct);
            }
            if (!constanciaRevocada || nuevaDecision)
            {
                await avisos.Trabajo(conexion, tr, id, actor, nuevaDecision ? "DECISION_BIBLIOTECA" : "CONTROLES_ACTUALIZADOS",
                    nuevaDecision ? "Biblioteca registró una decisión" : "Revisión del trabajo actualizada",
                    nuevaDecision ? $"Resultado: {datos.Decision!.Resultado}\nMotivo: {datos.Decision.Motivo}" : "Biblioteca guardó una actualización de los controles del ejemplar. Consulta el estado de tu envío.", false, ct);
            }
            await tr.CommitAsync(ct);
        }

        private static async Task GuardarDeposito(NpgsqlConnection c, NpgsqlTransaction tr, int id, int actor,
            int ejemplar, ControlesFicha datos, CancellationToken ct)
        {
            if (datos.DepositoReferencia is not null || datos.DepositoVerificado.HasValue)
            {
                string? referenciaAnterior = await Comandos.Valor<string>(c, tr,
                    "SELECT referencia_deposito_ues FROM public.trabajos WHERE id_trabajo=@id", ct, ("id", id));
                string? referencia = datos.DepositoReferencia is null ? referenciaAnterior : Fichas.TextoOpcional(datos.DepositoReferencia);
                bool cambio = referencia != referenciaAnterior;
                if (datos.DepositoVerificado == true && referencia is null)
                {
                    throw new ErrorApi(400, "Indica el comprobante UES antes de verificarlo.");
                }
                if ((cambio && referenciaAnterior is not null) || datos.DepositoVerificado == false)
                {
                    await Suspender(c, tr, id, actor, "Cambio o retiro de la verificación del depósito UES.", ct);
                }

                await Comandos.Ejecutar(c, tr, """
                    UPDATE public.trabajos SET referencia_deposito_ues=@ref::text,
                        deposito_verificado_por=CASE
                            WHEN @verificar AND (@cambio OR deposito_verificado_en IS NULL) THEN @actor
                            WHEN @limpiar THEN NULL ELSE deposito_verificado_por END,
                        deposito_verificado_en=CASE
                            WHEN @verificar AND (@cambio OR deposito_verificado_en IS NULL) THEN clock_timestamp()
                            WHEN @limpiar THEN NULL ELSE deposito_verificado_en END
                    WHERE id_trabajo=@id
                    """, ct, ("ref", referencia), ("cambio", cambio), ("limpiar", cambio || datos.DepositoVerificado == false),
                    ("verificar", datos.DepositoVerificado == true), ("actor", actor), ("id", id));
            }
            if (datos.DepositoFecha is not null)
            {
                DateOnly? fecha = Fecha(datos.DepositoFecha, "fecha de depósito");
                await Comandos.Ejecutar(c, tr, "UPDATE public.entregas SET fecha_deposito=@fecha::date WHERE id_entrega=@e",
                    ct, ("fecha", fecha), ("e", ejemplar));
            }
        }

        private static async Task GuardarConstancia(NpgsqlConnection c, NpgsqlTransaction tr, int ficha, int actor,
            int ejemplar, ConstanciaDatos datos, CancellationToken ct)
        {
            int activa = await Comandos.Valor<int>(c, tr,
                "SELECT id_autorizacion FROM public.autorizaciones_difusion WHERE id_entrega=@e AND estado='Activa'", ct, ("e", ejemplar));
            if (datos.Estado == "Revocada")
            {
                if (activa != 0)
                {
                    await Suspender(c, tr, ficha, actor, "Se revocó la constancia de difusión.", ct);
                    await Comandos.Ejecutar(c, tr, """
                        UPDATE public.autorizaciones_difusion SET estado='Revocada' WHERE id_autorizacion=@a;
                        UPDATE public.entregas SET difundir_ficha_habilitado=false, leer_pdf_habilitado=false,
                             descargar_pdf_habilitado=false WHERE id_entrega=@e;
                        """, ct, ("a", activa), ("e", ejemplar));
                }
                return;
            }
            if (datos.Estado is not ("Vigente" or "Activa"))
            {
                throw new ErrorApi(400, "El estado de la constancia debe ser Vigente o Revocada.");
            }

            string? referencia = Fichas.TextoOpcional(datos.Referencia);
            DateOnly? fecha = Fecha(datos.Fecha, "fecha de la constancia");
            DateOnly? vence = Fecha(datos.Vence, "vencimiento");
            int[] otorgantes = await Fichas.ResolverPersonas(c, tr, datos.OtorganteIds,
                datos.Otorgantes?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), ct);
            if (referencia is null || !fecha.HasValue || otorgantes.Length == 0 || vence < fecha)
            {
                throw new ErrorApi(400, "La constancia requiere referencia, fecha, al menos un otorgante registrado y vigencia válida.");
            }
            bool mismo = activa != 0 && await Comandos.Valor<bool>(c, tr, """
                SELECT coalesce((SELECT referencia_respaldo=@ref
                    AND (fecha_otorgamiento AT TIME ZONE 'America/El_Salvador')::date=@fecha::date
                    AND vigencia_hasta IS NOT DISTINCT FROM @vence::date
                    AND difundir_ficha_concedido=@ficha AND leer_pdf_concedido=@lectura AND descargar_pdf_concedido=@descarga
                    AND array(SELECT id_persona FROM public.personas_autorizaciones WHERE id_autorizacion=@a ORDER BY id_persona)=@ids
                    FROM public.autorizaciones_difusion WHERE id_autorizacion=@a),false)
                """, ct, ("ref", referencia), ("fecha", fecha.Value), ("vence", vence), ("ficha", datos.Ficha),
                ("lectura", datos.Lectura), ("descarga", datos.Descarga), ("ids", otorgantes.Order().ToArray()), ("a", activa));
            if (mismo)
            {
                return;
            }
            await Suspender(c, tr, ficha, actor, "Se registró una nueva constancia de difusión.", ct);
            await Comandos.Ejecutar(c, tr,
                "UPDATE public.autorizaciones_difusion SET estado='Sustituida' WHERE id_autorizacion=@a", ct, ("a", activa));
            int nueva = await Comandos.Valor<int>(c, tr, """
                INSERT INTO public.autorizaciones_difusion(id_entrega,id_usuario_registra,fecha_otorgamiento,alcance,estado,
                    referencia_respaldo,vigencia_hasta,difundir_ficha_concedido,leer_pdf_concedido,descargar_pdf_concedido)
                VALUES (@e,@actor,(@fecha::date::timestamp AT TIME ZONE 'America/El_Salvador'),'Difusión','Activa',
                    @ref,@vence::date,@ficha,@lectura,@descarga) RETURNING id_autorizacion
                """, ct, ("e", ejemplar), ("actor", actor), ("fecha", fecha.Value), ("ref", referencia), ("vence", vence),
                ("ficha", datos.Ficha), ("lectura", datos.Lectura), ("descarga", datos.Descarga));
            await Comandos.Ejecutar(c, tr, """
                INSERT INTO public.personas_autorizaciones(id_persona,id_autorizacion) SELECT unnest(@ids),@a
                """, ct, ("ids", otorgantes), ("a", nueva));
        }

        private static async Task GuardarPermisos(NpgsqlConnection c, NpgsqlTransaction tr, int ejemplar,
            ControlesFicha datos, CancellationToken ct)
        {
            if (datos.Autorizacion?.Estado == "Revocada")
            {
                return;
            }

            bool valido = await Comandos.Valor<bool>(c, tr, """
                SELECT NOT (coalesce(@ficha::bool,difundir_ficha_habilitado) OR coalesce(@lectura::bool,leer_pdf_habilitado)
                         OR coalesce(@descarga::bool,descargar_pdf_habilitado))
                    OR EXISTS(SELECT 1 FROM public.autorizaciones_difusion a
                        WHERE a.id_entrega=e.id_entrega AND a.estado='Activa'
                        AND a.fecha_otorgamiento<=clock_timestamp()
                        AND (a.vigencia_hasta IS NULL OR a.vigencia_hasta >= (clock_timestamp() AT TIME ZONE 'America/El_Salvador')::date)
                        AND (NOT coalesce(@ficha::bool,difundir_ficha_habilitado) OR a.difundir_ficha_concedido)
                        AND (NOT coalesce(@lectura::bool,leer_pdf_habilitado) OR a.leer_pdf_concedido)
                        AND (NOT coalesce(@descarga::bool,descargar_pdf_habilitado) OR a.descargar_pdf_concedido))
                FROM public.entregas e WHERE e.id_entrega=@e
                """, ct, ("ficha", datos.FichaPublica), ("lectura", datos.Lectura), ("descarga", datos.Descarga), ("e", ejemplar));
            if (!valido)
            {
                throw new ErrorApi(400, "Los permisos habilitados deben estar respaldados por la constancia vigente.");
            }
            await Comandos.Ejecutar(c, tr, """
                UPDATE public.entregas SET difundir_ficha_habilitado=coalesce(@ficha::bool,difundir_ficha_habilitado),
                    leer_pdf_habilitado=coalesce(@lectura::bool,leer_pdf_habilitado),
                    descargar_pdf_habilitado=coalesce(@descarga::bool,descargar_pdf_habilitado) WHERE id_entrega=@e
                """, ct, ("ficha", datos.FichaPublica), ("lectura", datos.Lectura), ("descarga", datos.Descarga), ("e", ejemplar));
        }

        public async Task Publicar(int id, int actor, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            bool canal = archivos.Ubicar(await ClaveFinal(conexion,tr,id,ct)) is not null;
            string[] bloqueos = await Comandos.Valor<string[]>(conexion, tr,
                "CALL public.sp_publicar_trabajo(@id,@actor,@canal,NULL::text[])", ct,
                ("id", id), ("actor", actor), ("canal", canal)) ?? Array.Empty<string>();
            if (bloqueos.Length > 0)
            {
                throw new ErrorApi(409, "La ficha aún no se puede publicar.",
                    new { error = "La ficha aún no se puede publicar.", bloqueos, controles = ResumenControles(bloqueos) });
            }
            await avisos.Trabajo(conexion, tr, id, actor, "TRABAJO_PUBLICADO", "Tu trabajo fue publicado",
                "Biblioteca publicó el trabajo. Ya está disponible en el catálogo con los permisos de lectura y descarga autorizados.", false, ct);
            await tr.CommitAsync(ct);
        }

        public async Task Retirar(int id, int actor, string? motivo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new ErrorApi(400, "Indica el motivo del retiro.");
            }
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlTransaction tr = await conexion.BeginTransactionAsync(ct);
            if (await Fichas.Bloquear(conexion, tr, id, ct) != "Publicado")
            {
                throw new ErrorApi(409, "Solo se puede retirar una ficha publicada.");
            }
            await Comandos.Ejecutar(conexion,tr,"CALL public.sp_retirar_trabajo(@id,@actor,@motivo)",ct,
                ("id",id),("actor",actor),("motivo",motivo.Trim()));
            await avisos.Trabajo(conexion, tr, id, actor, "PUBLICACION_RETIRADA", "Publicación del trabajo retirada",
                $"Biblioteca retiró la publicación.\nMotivo: {motivo.Trim()}", false, ct);
            await tr.CommitAsync(ct);
        }

        public async Task<string> PdfBiblioteca(int id, int? ejemplar, CancellationToken ct)
        {
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            string? clave = await Comandos.Valor<string>(conexion, null, """
                SELECT r.ubicacion FROM public.entregas e JOIN public.recursos_digitales r USING(id_entrega,id_trabajo)
                WHERE e.id_trabajo=@id AND e.numero_version=(SELECT max(numero_version) FROM public.entregas WHERE id_trabajo=@id)
                    AND (@e::int IS NULL OR e.id_entrega=@e::int)
                    AND r.clase='PDF' AND r.estado_carga='Disponible'
                """, ct, ("id", id), ("e", ejemplar));
            return archivos.Ubicar(clave) ?? throw new ErrorApi(404, "El ejemplar no tiene un PDF disponible.");
        }

        private async Task<string[]> Evaluar(NpgsqlConnection c, NpgsqlTransaction? tr, int id, CancellationToken ct)
        {
            bool canal = archivos.Ubicar(await ClaveFinal(c,tr,id,ct)) is not null;
            return await Comandos.Valor<string[]>(c,tr,"SELECT bloqueos FROM public.fn_elegibilidad_difusion(@id,@canal)",ct,
                ("id",id),("canal",canal)) ?? Array.Empty<string>();
        }

        private static async Task<string?> ClaveFinal(NpgsqlConnection c, NpgsqlTransaction? tr, int id, CancellationToken ct)
        {
            return await Comandos.Valor<string>(c,tr, """
                SELECT r.ubicacion FROM public.trabajos t JOIN public.recursos_digitales r
                ON r.id_trabajo=t.id_trabajo AND r.id_entrega=t.id_entrega_final
                WHERE t.id_trabajo=@id AND r.clase='PDF' AND r.estado_carga='Disponible'
                """,ct,("id",id));
        }

        private static async Task Suspender(NpgsqlConnection c, NpgsqlTransaction tr, int id, int actor, string motivo, CancellationToken ct)
        {
            await Comandos.Ejecutar(c,tr,"CALL public.sp_suspender_difusion(@id,@actor,@motivo)",ct,
                ("id",id),("actor",actor),("motivo",motivo));
        }

        private static List<object> ResumenControles(string[] bloqueos)
        {
            List<object> respuesta = new List<object>();
            for (int i = 0; i < Controles.Length; i++)
            {
                string prefijo = (i + 1).ToString("D2") + "_";
                respuesta.Add(new { label = Controles[i], ok = !bloqueos.Any(b => b.StartsWith(prefijo, StringComparison.Ordinal)) });
            }
            return respuesta;
        }

        private static DateOnly? Fecha(string? dato, string campo)
        {
            if (string.IsNullOrWhiteSpace(dato))
            {
                return null;
            }
            if (!DateOnly.TryParseExact(dato, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly fecha))
            {
                throw new ErrorApi(400, "Revisa la " + campo + ".");
            }
            return fecha;
        }
    }
}
