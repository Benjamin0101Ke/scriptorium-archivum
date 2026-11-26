using System.Security.Claims;
using APIScriptoriumArchivum.Data;
using APIScriptoriumArchivum.Datos;
using APIScriptoriumArchivum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace APIScriptoriumArchivum.Controllers
{
    // Administra los catalogos conservando el contrato de la interfaz
    [ApiController]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class CatalogosAdminController : ControllerBase
    {
        // Contexto EF utilizado para consultar y actualizar los catalogos
        private AppDbContext contexto;
        // Recibe el contexto EF que consulta y actualiza los catalogos
        public CatalogosAdminController(AppDbContext contexto)
        {
            this.contexto = contexto;
        }

        // Obtiene la oferta academica habilitada para los formularios
        [HttpGet("api/oferta-academica")]
        public async Task<IActionResult> Oferta(CancellationToken ct)
        {
            var departamentos = await contexto.Departamentos.AsNoTracking().Where(d => d.Activa).OrderBy(d => d.Nombre).Select(d => new { id = d.IdDepartamento, nombre = d.Nombre }).ToListAsync(ct);
            var carreras = await (
                from c in contexto.Carreras.AsNoTracking()join d in contexto.Departamentos on c.IdDepartamento equals d.IdDepartamento
                where c.Activa && d.Activa
                orderby c.Nombre
                select new
                {
                    id = c.IdCarrera,
                    nombre = c.Nombre,
                    id_departamento = c.IdDepartamento,
                    id_area = c.IdDepartamento
                }

            ).ToListAsync(ct);
            var planes = await (
                from p in contexto.PlanesEstudio.AsNoTracking()join c in contexto.Carreras on p.IdCarrera equals c.IdCarrera
                join d in contexto.Departamentos on c.IdDepartamento equals d.IdDepartamento
                where p.Activa && c.Activa && d.Activa
                orderby p.Nombre
                select new
                {
                    id = p.IdPlan,
                    nombre = p.Nombre,
                    id_carrera = p.IdCarrera,
                    url = p.DocumentoUrl
                }

            ).ToListAsync(ct);
            return Ok(new { departamentos, areas = departamentos, carreras, planes });
        }

        // Identifica la tabla y la longitud maxima del nombre
        private static (string Tabla, int Maximo) Catalogo(string tipo)
        {
            switch (tipo)
            {
                case "carreras":
                    return ("carreras", 120);
                case "departamentos":
                case "areas-academicas":
                    return ("departamentos", 100);
                case "areas-tematicas":
                    return ("areas_tematicas", 100);
                case "planes":
                    return ("planes_estudio", 120);
                case "tipos-trabajo":
                    return ("tipos_trabajo", 60);
                default:
                    throw new ErrorApi(404, "El catálogo no existe.");
            }
        }

        // Lista los registros con el mismo formato JSON existente
        [Authorize(Roles = "Administrador")]
        [HttpGet("api/admin/catalogos/{tipo}")]
        public async Task<IActionResult> Listar(string tipo, CancellationToken ct)
        {
            Catalogo(tipo);
            switch (tipo)
            {
                case "carreras":
                    return Ok(await contexto.Carreras.AsNoTracking().OrderBy(c => c.Nombre).Select(c => new { id = c.IdCarrera, nombre = c.Nombre, activa = c.Activa, id_departamento = c.IdDepartamento, id_area = c.IdDepartamento, codigo = c.CodigoCarrera }).ToListAsync(ct));
                case "planes":
                    return Ok(await contexto.PlanesEstudio.AsNoTracking().OrderBy(p => p.Nombre).Select(p => new { id = p.IdPlan, nombre = p.Nombre, activa = p.Activa, id_carrera = p.IdCarrera, url = p.DocumentoUrl }).ToListAsync(ct));
                case "departamentos":
                case "areas-academicas":
                    return Ok(await contexto.Departamentos.AsNoTracking().OrderBy(d => d.Nombre).Select(d => new { id = d.IdDepartamento, nombre = d.Nombre, activa = d.Activa }).ToListAsync(ct));
                case "areas-tematicas":
                    return Ok(await contexto.AreasTematicas.AsNoTracking().OrderBy(a => a.Nombre).Select(a => new { id = a.IdArea, nombre = a.Nombre, activa = a.Activa }).ToListAsync(ct));
                default:
                    return Ok(await contexto.TiposTrabajo.AsNoTracking().OrderBy(t => t.Nombre).Select(t => new { id = t.IdTipoTrabajo, nombre = t.Nombre, activa = t.Activa }).ToListAsync(ct));
            }
        }

        // Crea un registro del catalogo solicitado
        [Authorize(Roles = "Administrador")]
        [HttpPost("api/admin/catalogos/{tipo}")]
        public async Task<IActionResult> Crear(string tipo, DatosCatalogo datos, CancellationToken ct)
        {
            return await Guardar(tipo, null, datos, ct);
        }

        // Edita los datos o desactiva el registro sin eliminar su historial
        [Authorize(Roles = "Administrador")]
        [HttpPut("api/admin/catalogos/{tipo}/{id:int}")]
        public async Task<IActionResult> Editar(string tipo, int id, DatosCatalogo datos, CancellationToken ct)
        {
            return await Guardar(tipo, id, datos, ct);
        }

        // Guarda el registro y su bitacora dentro de una transaccion
        private async Task<IActionResult> Guardar(string tipo, int? id, DatosCatalogo datos, CancellationToken ct)
        {
            var(tabla, maximo) = Catalogo(tipo);
            string nombre = datos.Nombre?.Trim() ?? "";
            if (nombre.Length == 0 || nombre.Length > maximo || nombre.Any(char.IsControl))
            {
                throw new ErrorApi(400, $"Escribe un nombre de hasta {maximo} caracteres.");
            }

            int? departamento = datos.IdDepartamento ?? datos.IdArea;
            string? documento = string.IsNullOrWhiteSpace(datos.Url) ? null : datos.Url.Trim();
            if (documento is not null && (!Uri.TryCreate(documento, UriKind.Absolute, out var url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo) || documento.Length > 2000))
            {
                throw new ErrorApi(400, "Ingresa un enlace HTTPS válido al documento del plan.");
            }

            await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);
            if (tipo == "planes")
            {
                if (!await contexto.Carreras.AnyAsync(c => c.IdCarrera == datos.IdCarrera, ct))
                {
                    throw new ErrorApi(400, "Selecciona la carrera del plan.");
                }

                if (await contexto.Usuarios.AnyAsync(u => u.IdPlan == id && u.IdPlan != null && u.IdCarrera != null && u.IdCarrera != datos.IdCarrera, ct))
                {
                    throw new ErrorApi(409, "Este plan ya tiene estudiantes de otra carrera. Crea un plan para la nueva carrera.");
                }
            }

            if (tipo == "carreras" && !await contexto.Departamentos.AnyAsync(d => d.IdDepartamento == departamento, ct))
            {
                throw new ErrorApi(400, "Selecciona un departamento.");
            }

            try
            {
                object registro;
                string propiedadId;
                switch (tipo)
                {
                    case "carreras":
                        Carreras carrera = new Carreras();
                        if (id.HasValue)
                        {
                            carrera = await contexto.Carreras.FindAsync(new object[] { id.Value }, ct) ?? throw new ErrorApi(404, "El registro no existe.");
                        }
                        else
                        {
                            carrera.CodigoCarrera = "LOC-" + Guid.NewGuid().ToString("N")[..12];
                            contexto.Carreras.Add(carrera);
                        }

                        carrera.Nombre = nombre;
                        carrera.Activa = datos.Activa;
                        carrera.IdDepartamento = departamento;
                        registro = carrera;
                        propiedadId = nameof(Carreras.IdCarrera);
                        break;
                    case "planes":
                        PlanesEstudio plan = new PlanesEstudio();
                        if (id.HasValue)
                        {
                            plan = await contexto.PlanesEstudio.FindAsync(new object[] { id.Value }, ct) ?? throw new ErrorApi(404, "El registro no existe.");
                        }
                        else
                        {
                            contexto.PlanesEstudio.Add(plan);
                        }

                        plan.Nombre = nombre;
                        plan.Activa = datos.Activa;
                        plan.IdCarrera = datos.IdCarrera!.Value;
                        plan.DocumentoUrl = documento;
                        registro = plan;
                        propiedadId = nameof(PlanesEstudio.IdPlan);
                        break;
                    case "departamentos":
                    case "areas-academicas":
                        Departamentos area = new Departamentos();
                        if (id.HasValue)
                        {
                            area = await contexto.Departamentos.FindAsync(new object[] { id.Value }, ct) ?? throw new ErrorApi(404, "El registro no existe.");
                        }
                        else
                        {
                            contexto.Departamentos.Add(area);
                        }

                        area.Nombre = nombre;
                        area.Activa = datos.Activa;
                        registro = area;
                        propiedadId = nameof(Departamentos.IdDepartamento);
                        break;
                    case "areas-tematicas":
                        AreasTematicas tema = new AreasTematicas();
                        if (id.HasValue)
                        {
                            tema = await contexto.AreasTematicas.FindAsync(new object[] { id.Value }, ct) ?? throw new ErrorApi(404, "El registro no existe.");
                        }
                        else
                        {
                            contexto.AreasTematicas.Add(tema);
                        }

                        tema.Nombre = nombre;
                        tema.Activa = datos.Activa;
                        registro = tema;
                        propiedadId = nameof(AreasTematicas.IdArea);
                        break;
                    default:
                        TiposTrabajo trabajo = new TiposTrabajo();
                        if (id.HasValue)
                        {
                            trabajo = await contexto.TiposTrabajo.FindAsync(new object[] { id.Value }, ct) ?? throw new ErrorApi(404, "El registro no existe.");
                        }
                        else
                        {
                            contexto.TiposTrabajo.Add(trabajo);
                        }

                        trabajo.Nombre = nombre;
                        trabajo.Activa = datos.Activa;
                        registro = trabajo;
                        propiedadId = nameof(TiposTrabajo.IdTipoTrabajo);
                        break;
                }

                await contexto.SaveChangesAsync(ct);
                int resultado = (int)contexto.Entry(registro).Property(propiedadId).CurrentValue!;
                contexto.Bitacora.Add(new Bitacora { IdUsuarioActor = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Operacion = "GUARDAR_CATALOGO", TablaAfectada = tabla, RegistroAfectado = resultado.ToString(), Detalle = nombre });
                await contexto.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
                return Ok(new { id = resultado });
            }
            catch (DbUpdateException e)when (e.InnerException is PostgresException pg && pg.SqlState == "23505")
            {
                throw new ErrorApi(409, "Ya existe un registro con ese nombre.");
            }
        }

        // Valores recibidos al crear o editar un registro de los catalogos
        public class DatosCatalogo
        {
            public string? Nombre { get; set; }
            public int? IdArea { get; set; }
            public bool Activa { get; set; } = true;
            public int? IdCarrera { get; set; }
            public string? Url { get; set; }
            public int? IdDepartamento { get; set; }
        }
    }
}
