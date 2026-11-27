using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Ejemplares
    {
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private ArchivoPdf archivos;
        private Avisos avisos;
        private static string[] Controles =
        {
            "Metadatos completos",
            "Comprobante UES verificado",
            "Ejemplar final verificado",
            "Constancia vigente para el ejemplar",
            "Decisión favorable de Biblioteca",
            "Ficha pública autorizada",
            "Permisos de PDF respaldados",
            "PDF disponible y controlado",
            "Cuenta de Biblioteca y estado válido"
        };
        public Ejemplares(IDbContextFactory<AppDbContext> baseDeDatos, ArchivoPdf archivos, Avisos avisos)
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
            string sql = "SELECT jsonb_build_object(" + Fichas.DatosBasicos + ", " + detalle + ")::text " + Fichas.RelacionesBasicas + " " + joins;
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            string? datos = await Comandos.Valor<string>(conexion, null, sql, ct, ("id", id));
            if (datos is null)
            {
                throw new ErrorApi(404, "La ficha no existe.");
            }

            JsonObject respuesta = JsonNode.Parse(datos)!.AsObject();
            string[] bloqueos = await Evaluar(conexion, null, id, ct);
            if (respuesta["estado"]?.GetValue<string>()is "Publicado" or "Retirado")
            {
                bloqueos = bloqueos.Append("09_ESTADO_ORIGEN_INVALIDO").ToArray();
            }

            respuesta["controles"] = JsonSerializer.SerializeToNode(ResumenControles(bloqueos));
            return respuesta;
        }

        public async Task<int> Registrar(int id, int actor, string clase, string ubicacion, string? nombre, long? bytes, CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            await Suspender(conexion, tr, id, actor, "Sustitución del ejemplar.", ct);
            await conexion.Trabajos.Where(t => t.IdTrabajo == id).ExecuteUpdateAsync(s => s.SetProperty(t => t.IdEntregaFinal, (int? )null).SetProperty(t => t.DepositoVerificadoPor, (int? )null).SetProperty(t => t.DepositoVerificadoEn, (DateTime? )null), ct);
            await conexion.Entregas.Where(e => e.IdTrabajo == id && e.Estado != "Sustituido").ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, "Sustituido"), ct);
            int ejemplar = await AgregarVersion(conexion, tr, id, actor, clase, ubicacion, nombre, bytes, ct);
            await avisos.Trabajo(conexion, tr, id, actor, "EJEMPLAR_ACTUALIZADO", "Ejemplar del trabajo actualizado", "Biblioteca asoció un nuevo ejemplar. Este ejemplar requiere sus propias verificaciones, autorización y decisión.", false, ct);
            await tr.CommitAsync(CancellationToken.None);
            return ejemplar;
        }

        internal static async Task<int> AgregarVersion(AppDbContext conexion, IDbContextTransaction tr, int id, int actor, string clase, string ubicacion, string? nombre, long? bytes, CancellationToken ct)
        {
            int version = await conexion.Entregas.Where(e => e.IdTrabajo == id).Select(e => (int? )e.NumeroVersion).MaxAsync(ct) ?? 0;
            var entrega = new APIScriptoriumArchivum.Models.Entregas
            {
                IdTrabajo = id,
                NumeroVersion = version + 1,
                IdUsuarioEnvia = actor
            };
            conexion.Entregas.Add(entrega);
            await conexion.SaveChangesAsync(ct);
            int ejemplar = entrega.IdEntrega;
            conexion.RecursosDigitales.Add(new APIScriptoriumArchivum.Models.RecursosDigitales { IdTrabajo = id, IdEntrega = ejemplar, Clase = clase, Ubicacion = ubicacion, Nombre = nombre, TamanoBytes = bytes, TipoFormato = clase == "PDF" ? "application/pdf" : clase == "ENLACE" ? "text/html" : null, EstadoCarga = clase == "PDF" ? "Disponible" : null, Descripcion = clase == "ENLACE" ? "Repositorio externo del proyecto de software" : null });
            await conexion.SaveChangesAsync(ct);
            return ejemplar;
        }

        public async Task GuardarControles(int id, int actor, ControlesFicha datos, CancellationToken ct)
        {
            if (!int.TryParse(datos.EjemplarId, out int ejemplar))
            {
                throw new ErrorApi(400, "Indica el ejemplar.");
            }

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            int actual = await Comandos.Valor<int>(conexion, tr, "SELECT id_entrega FROM public.entregas WHERE id_trabajo=@id ORDER BY numero_version DESC LIMIT 1", ct, ("id", id));
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

                string? previa = await Comandos.Valor<string>(conexion, tr, "SELECT referencia_conformidad FROM public.entregas WHERE id_entrega=@e", ct, ("e", ejemplar));
                if (previa is not null && previa != conformidad)
                {
                    await Suspender(conexion, tr, id, actor, "Cambio de la referencia de conformidad.", ct);
                }

                await conexion.Entregas.Where(e => e.IdEntrega == ejemplar).ExecuteUpdateAsync(s => s.SetProperty(e => e.Estado, "Verificado").SetProperty(e => e.ReferenciaConformidad, conformidad).SetProperty(e => e.VerificadoPor, actor).SetProperty(e => e.VerificadoEn, DateTime.UtcNow), ct);
                await conexion.Trabajos.Where(t => t.IdTrabajo == id).ExecuteUpdateAsync(s => s.SetProperty(t => t.IdEntregaFinal, ejemplar), ct);
            }

            if (datos.Autorizacion is not null || datos.Decision is not null || datos.FichaPublica == true || datos.Lectura == true || datos.Descarga == true)
            {
                bool verificado = await Comandos.Valor<bool>(conexion, tr, "SELECT EXISTS(SELECT 1 FROM public.entregas WHERE id_entrega=@e AND estado='Verificado')", ct, ("e", ejemplar));
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
                    DateTime ahora = DateTime.UtcNow;
                    conexion.Revisiones.Add(new APIScriptoriumArchivum.Models.Revisiones { IdEntrega = ejemplar, IdUsuarioRevisor = actor, FechaInicio = ahora, FechaCierre = ahora, AmbitoRevision = "Difusión", Resultado = datos.Decision.Resultado, MotivoDecision = razon });
                    await conexion.SaveChangesAsync(ct);
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

        private static async Task GuardarDeposito(AppDbContext c, IDbContextTransaction tr, int id, int actor, int ejemplar, ControlesFicha datos, CancellationToken ct)
        {
            if (datos.DepositoReferencia is not null || datos.DepositoVerificado.HasValue)
            {
                string? referenciaAnterior = await Comandos.Valor<string>(c, tr, "SELECT referencia_deposito_ues FROM public.trabajos WHERE id_trabajo=@id", ct, ("id", id));
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

                var trabajo = await c.Trabajos.SingleAsync(t => t.IdTrabajo == id, ct);
                trabajo.ReferenciaDepositoUes = referencia;
                if (datos.DepositoVerificado == true && (cambio || trabajo.DepositoVerificadoEn == null))
                {
                    trabajo.DepositoVerificadoPor = actor;
                    trabajo.DepositoVerificadoEn = DateTime.UtcNow;
                }
                else if (cambio || datos.DepositoVerificado == false)
                {
                    trabajo.DepositoVerificadoPor = null;
                    trabajo.DepositoVerificadoEn = null;
                }

                await c.SaveChangesAsync(ct);
            }

            if (datos.DepositoFecha is not null)
            {
                DateOnly? fecha = Fecha(datos.DepositoFecha, "fecha de depósito");
                await c.Entregas.Where(e => e.IdEntrega == ejemplar).ExecuteUpdateAsync(s => s.SetProperty(e => e.FechaDeposito, fecha), ct);
            }
        }

        private static async Task GuardarConstancia(AppDbContext c, IDbContextTransaction tr, int ficha, int actor, int ejemplar, ConstanciaDatos datos, CancellationToken ct)
        {
            int activa = await Comandos.Valor<int>(c, tr, "SELECT id_autorizacion FROM public.autorizaciones_difusion WHERE id_entrega=@e AND estado='Activa'", ct, ("e", ejemplar));
            if (datos.Estado == "Revocada")
            {
                // Comprueba que exista una constancia registrada antes de revocarla
                int registrada = activa;
                if (registrada == 0)
                {
                    registrada = await Comandos.Valor<int>(c, tr,
                        "SELECT id_autorizacion FROM public.autorizaciones_difusion WHERE id_entrega=@e AND estado='Revocada' ORDER BY id_autorizacion DESC LIMIT 1",
                        ct, ("e", ejemplar));
                }
                if (registrada == 0)
                {
                    throw new ErrorApi(400, "Este ejemplar no tiene una constancia registrada para revocar. Guarda primero la referencia, los otorgantes y la fecha con estado Vigente.");
                }
                if (datos.Referencia is not null || datos.OtorganteIds is not null || !string.IsNullOrWhiteSpace(datos.Otorgantes))
                {
                    // La revocacion conserva la referencia y las personas que otorgaron la autorizacion
                    int[]? personas = null;
                    if (datos.OtorganteIds is not null || !string.IsNullOrWhiteSpace(datos.Otorgantes))
                    {
                        personas = await Fichas.ResolverPersonas(c, tr, datos.OtorganteIds,
                            datos.Otorgantes?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), ct);
                    }
                    bool coinciden = await Comandos.Valor<bool>(c, tr,
                        "SELECT (@ref::text IS NULL OR referencia_respaldo=@ref) AND (@ids::int[] IS NULL OR array(SELECT id_persona FROM public.personas_autorizaciones WHERE id_autorizacion=@a ORDER BY id_persona)=@ids) FROM public.autorizaciones_difusion WHERE id_autorizacion=@a",
                        ct, ("ref", datos.Referencia?.Trim()), ("ids", personas?.Order().ToArray()), ("a", registrada));
                    if (!coinciden)
                    {
                        throw new ErrorApi(400, "Los otorgantes o la referencia cambiaron. Guarda primero la constancia con estado Vigente y después revócala; la revocación conserva los datos de la constancia registrada.");
                    }
                }
                if (activa != 0)
                {
                    await Suspender(c, tr, ficha, actor, "Se revocó la constancia de difusión.", ct);
                    await c.AutorizacionesDifusion.Where(a => a.IdAutorizacion == activa).ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, "Revocada"), ct);
                    await c.Entregas.Where(e => e.IdEntrega == ejemplar).ExecuteUpdateAsync(s => s.SetProperty(e => e.DifundirFichaHabilitado, false).SetProperty(e => e.LeerPdfHabilitado, false).SetProperty(e => e.DescargarPdfHabilitado, false), ct);
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
            int[] otorgantes = await Fichas.ResolverPersonas(c, tr, datos.OtorganteIds, datos.Otorgantes?.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), ct);
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
                """, ct, ("ref", referencia), ("fecha", fecha.Value), ("vence", vence), ("ficha", datos.Ficha), ("lectura", datos.Lectura), ("descarga", datos.Descarga), ("ids", otorgantes.Order().ToArray()), ("a", activa));
            if (mismo)
            {
                return;
            }

            await Suspender(c, tr, ficha, actor, "Se registró una nueva constancia de difusión.", ct);
            await c.AutorizacionesDifusion.Where(a => a.IdAutorizacion == activa).ExecuteUpdateAsync(s => s.SetProperty(a => a.Estado, "Sustituida"), ct);
            var autorizacion = new APIScriptoriumArchivum.Models.AutorizacionesDifusion
            {
                IdEntrega = ejemplar,
                IdUsuarioRegistra = actor,
                FechaOtorgamiento = TimeZoneInfo.ConvertTimeToUtc(fecha.Value.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.FindSystemTimeZoneById("America/El_Salvador")),
                Alcance = "Difusión",
                Estado = "Activa",
                ReferenciaRespaldo = referencia,
                VigenciaHasta = vence,
                DifundirFichaConcedido = datos.Ficha,
                LeerPdfConcedido = datos.Lectura,
                DescargarPdfConcedido = datos.Descarga
            };
            c.AutorizacionesDifusion.Add(autorizacion);
            await c.SaveChangesAsync(ct);
            foreach (int persona in otorgantes)
            {
                c.PersonasAutorizaciones.Add(new APIScriptoriumArchivum.Models.PersonasAutorizaciones { IdPersona = persona, IdAutorizacion = autorizacion.IdAutorizacion });
            }

            await c.SaveChangesAsync(ct);
        }

        private static async Task GuardarPermisos(AppDbContext c, IDbContextTransaction tr, int ejemplar, ControlesFicha datos, CancellationToken ct)
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

            await c.Entregas.Where(e => e.IdEntrega == ejemplar).ExecuteUpdateAsync(s => s.SetProperty(e => e.DifundirFichaHabilitado, e => datos.FichaPublica ?? e.DifundirFichaHabilitado).SetProperty(e => e.LeerPdfHabilitado, e => datos.Lectura ?? e.LeerPdfHabilitado).SetProperty(e => e.DescargarPdfHabilitado, e => datos.Descarga ?? e.DescargarPdfHabilitado), ct);
        }

        public async Task Publicar(int id, int actor, CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            await Fichas.Bloquear(conexion, tr, id, ct);
            bool canal = archivos.Ubicar(await ClaveFinal(conexion, tr, id, ct))is not null;
            string[] bloqueos = await Comandos.Valor<string[]>(conexion, tr, "CALL public.sp_publicar_trabajo(@id,@actor,@canal,NULL::text[])", ct, ("id", id), ("actor", actor), ("canal", canal)) ?? Array.Empty<string>();
            if (bloqueos.Length > 0)
            {
                throw new ErrorApi(409, "La ficha aún no se puede publicar.", new { error = "La ficha aún no se puede publicar.", bloqueos, controles = ResumenControles(bloqueos) });
            }

            await avisos.Trabajo(conexion, tr, id, actor, "TRABAJO_PUBLICADO", "Tu trabajo fue publicado", "Biblioteca publicó el trabajo. Ya está disponible en el catálogo con los permisos de lectura y descarga autorizados.", false, ct);
            await tr.CommitAsync(ct);
        }

        public async Task Retirar(int id, int actor, string? motivo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                throw new ErrorApi(400, "Indica el motivo del retiro.");
            }

            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction tr = await conexion.Database.BeginTransactionAsync(ct);
            if (await Fichas.Bloquear(conexion, tr, id, ct) != "Publicado")
            {
                throw new ErrorApi(409, "Solo se puede retirar una ficha publicada.");
            }

            await Comandos.Ejecutar(conexion, tr, "CALL public.sp_retirar_trabajo(@id,@actor,@motivo)", ct, ("id", id), ("actor", actor), ("motivo", motivo.Trim()));
            await avisos.Trabajo(conexion, tr, id, actor, "PUBLICACION_RETIRADA", "Publicación del trabajo retirada", $"Biblioteca retiró la publicación.\nMotivo: {motivo.Trim()}", false, ct);
            await tr.CommitAsync(ct);
        }

        public async Task<string> PdfBiblioteca(int id, int? ejemplar, CancellationToken ct)
        {
            await using AppDbContext conexion = await baseDeDatos.CreateDbContextAsync(ct);
            string? clave = await Comandos.Valor<string>(conexion, null, """
                SELECT r.ubicacion FROM public.entregas e JOIN public.recursos_digitales r USING(id_entrega,id_trabajo)
                WHERE e.id_trabajo=@id AND e.numero_version=(SELECT max(numero_version) FROM public.entregas WHERE id_trabajo=@id)
                    AND (@e::int IS NULL OR e.id_entrega=@e::int)
                    AND r.clase='PDF' AND r.estado_carga='Disponible'
                """, ct, ("id", id), ("e", ejemplar));
            return archivos.Ubicar(clave) ?? throw new ErrorApi(404, "El ejemplar no tiene un PDF disponible.");
        }

        private async Task<string[]> Evaluar(AppDbContext c, IDbContextTransaction? tr, int id, CancellationToken ct)
        {
            bool canal = archivos.Ubicar(await ClaveFinal(c, tr, id, ct))is not null;
            return await Comandos.Valor<string[]>(c, tr, "SELECT bloqueos FROM public.fn_elegibilidad_difusion(@id,@canal)", ct, ("id", id), ("canal", canal)) ?? Array.Empty<string>();
        }

        private static async Task<string?> ClaveFinal(AppDbContext c, IDbContextTransaction? tr, int id, CancellationToken ct)
        {
            return await Comandos.Valor<string>(c, tr, """
                SELECT r.ubicacion FROM public.trabajos t JOIN public.recursos_digitales r
                ON r.id_trabajo=t.id_trabajo AND r.id_entrega=t.id_entrega_final
                WHERE t.id_trabajo=@id AND r.clase='PDF' AND r.estado_carga='Disponible'
                """, ct, ("id", id));
        }

        private static async Task Suspender(AppDbContext c, IDbContextTransaction tr, int id, int actor, string motivo, CancellationToken ct)
        {
            await Comandos.Ejecutar(c, tr, "CALL public.sp_suspender_difusion(@id,@actor,@motivo)", ct, ("id", id), ("actor", actor), ("motivo", motivo));
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
