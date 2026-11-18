using System.Security.Claims;
using APIScriptoriumArchivum.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CatalogosAdminController(NpgsqlDataSource db) : ControllerBase
{
    [HttpGet("api/oferta-academica")]
    public async Task<IActionResult> Oferta(CancellationToken ct)
    {
        await using var c = await db.OpenConnectionAsync(ct);
        var departamentos = await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',id_departamento,'nombre',nombre)::text
            FROM public.departamentos WHERE activa ORDER BY nombre
            """, ct);
        var carreras = await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',c.id_carrera,'nombre',c.nombre,'id_departamento',c.id_departamento,'id_area',c.id_departamento)::text
            FROM public.carreras c JOIN public.departamentos a USING(id_departamento)
            WHERE c.activa AND a.activa ORDER BY c.nombre
            """, ct);
        var planes = await Comandos.FilasJson(c, """
            SELECT jsonb_build_object('id',p.id_plan,'nombre',p.nombre,'id_carrera',p.id_carrera,'url',p.documento_url)::text
            FROM public.planes_estudio p JOIN public.carreras c USING(id_carrera)
            JOIN public.departamentos a USING(id_departamento)
            WHERE p.activa AND c.activa AND a.activa ORDER BY p.nombre
            """, ct);
        return Ok(new { departamentos, areas = departamentos, carreras, planes });
    }

    private static (string Tabla, string Id, int Maximo) Catalogo(string tipo) => tipo switch
    {
        "carreras" => ("carreras", "id_carrera", 120),
        "departamentos" or "areas-academicas" => ("departamentos", "id_departamento", 100),
        "areas-tematicas" => ("areas_tematicas", "id_area", 100),
        "planes" => ("planes_estudio", "id_plan", 120),
        "tipos-trabajo" => ("tipos_trabajo", "id_tipo_trabajo", 60),
        _ => throw new ErrorApi(404, "El catálogo no existe.")
    };

    [Authorize(Roles = "Administrador")]
    [HttpGet("api/admin/catalogos/{tipo}")]
    public async Task<IActionResult> Listar(string tipo, CancellationToken ct)
    {
        var (tabla, id, _) = Catalogo(tipo);
        string area = tipo == "carreras" ? ", 'id_departamento',id_departamento,'id_area',id_departamento,'codigo',codigo_carrera" : "";
        if (tipo == "planes") area = ", 'id_carrera',id_carrera,'url',documento_url";
        await using var c = await db.OpenConnectionAsync(ct);
        return Ok(await Comandos.FilasJson(c,
            $"SELECT jsonb_build_object('id',{id},'nombre',nombre,'activa',activa{area})::text FROM public.{tabla} ORDER BY nombre", ct));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost("api/admin/catalogos/{tipo}")]
    public Task<IActionResult> Crear(string tipo, DatosCatalogo datos, CancellationToken ct) => Guardar(tipo, null, datos, ct);

    [Authorize(Roles = "Administrador")]
    [HttpPut("api/admin/catalogos/{tipo}/{id:int}")]
    public Task<IActionResult> Editar(string tipo, int id, DatosCatalogo datos, CancellationToken ct) => Guardar(tipo, id, datos, ct);

    private async Task<IActionResult> Guardar(string tipo, int? id, DatosCatalogo datos, CancellationToken ct)
    {
        var (tabla, columna, maximo) = Catalogo(tipo);
        string nombre = datos.Nombre?.Trim() ?? "";
        if (nombre.Length == 0 || nombre.Length > maximo || nombre.Any(char.IsControl))
            throw new ErrorApi(400, $"Escribí un nombre de hasta {maximo} caracteres.");
        await using var c = await db.OpenConnectionAsync(ct);
        await using var tr = await c.BeginTransactionAsync(ct);
        int? departamento = datos.IdDepartamento ?? datos.IdArea;
        string? documento = string.IsNullOrWhiteSpace(datos.Url) ? null : datos.Url.Trim();
        if (documento is not null && (!Uri.TryCreate(documento, UriKind.Absolute, out var url) ||
            url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo) || documento.Length > 2000))
            throw new ErrorApi(400, "Ingresá un enlace HTTPS válido al documento del plan.");
        if (tipo == "planes")
        {
            bool existeCarrera = await Comandos.Valor<bool>(c, tr,
                "SELECT EXISTS(SELECT 1 FROM public.carreras WHERE id_carrera=@carrera)", ct, ("carrera", datos.IdCarrera));
            if (!existeCarrera) throw new ErrorApi(400, "Seleccioná la carrera del plan.");
            bool incompatible = await Comandos.Valor<bool>(c, tr, """
                SELECT EXISTS(SELECT 1 FROM public.usuarios WHERE id_plan=@id AND id_carrera<>@carrera)
                """, ct, ("id", id), ("carrera", datos.IdCarrera));
            if (incompatible) throw new ErrorApi(409, "Este plan ya tiene estudiantes de otra carrera. Creá un plan para la nueva carrera.");
        }
        if (tipo == "carreras")
        {
            bool existe = await Comandos.Valor<bool>(c, tr,
                "SELECT EXISTS(SELECT 1 FROM public.departamentos WHERE id_departamento=@area)",
                ct, ("area", departamento));
            if (!existe) throw new ErrorApi(400, "Seleccioná un departamento.");
        }
        string sql;
        if (id.HasValue)
        {
            string extra = tipo == "carreras" ? ", id_departamento=@area" : "";
            if (tipo == "planes") extra = ", id_carrera=@carrera,documento_url=@documento";
            sql = $"UPDATE public.{tabla} SET nombre=@nombre,activa=@activa{extra} WHERE {columna}=@id RETURNING {columna}";
        }
        else
        {
            string campos = tipo == "carreras" ? ",codigo_carrera,id_departamento" : "";
            string valores = tipo == "carreras" ? ",@codigo,@area" : "";
            if (tipo == "planes") { campos = ",id_carrera,documento_url"; valores = ",@carrera,@documento"; }
            sql = $"INSERT INTO public.{tabla}(nombre,activa{campos}) VALUES(@nombre,@activa{valores}) RETURNING {columna}";
        }
        try
        {
            int resultado = await Comandos.Valor<int>(c, tr, sql, ct, ("id", id), ("nombre", nombre),
                ("activa", datos.Activa), ("area", departamento), ("codigo", "LOC-" + Guid.NewGuid().ToString("N")[..12]),
                ("carrera", datos.IdCarrera), ("documento", documento));
            if (resultado == 0) throw new ErrorApi(404, "El registro no existe.");
            await Comandos.Ejecutar(c, tr, """
                INSERT INTO public.bitacora(id_usuario_actor,operacion,tabla_afectada,registro_afectado,detalle)
                VALUES(@actor,'GUARDAR_CATALOGO',@tabla,@id::text,@detalle)
                """, ct, ("actor", int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)),
                ("tabla", tabla), ("id", resultado), ("detalle", nombre));
            await tr.CommitAsync(ct);
            return Ok(new { id = resultado });
        }
        catch (PostgresException e) when (e.SqlState == "23505")
        {
            throw new ErrorApi(409, "Ya existe un registro con ese nombre.");
        }
    }

    public record DatosCatalogo(string? Nombre, int? IdArea, bool Activa = true, int? IdCarrera = null, string? Url = null, int? IdDepartamento = null);
}
