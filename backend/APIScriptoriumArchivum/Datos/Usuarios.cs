using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PersonaModelo = APIScriptoriumArchivum.Models.Personas;
using UsuarioModelo = APIScriptoriumArchivum.Models.Usuarios;

namespace APIScriptoriumArchivum.Datos
{
    public class Usuarios
    {
        private IDbContextFactory<AppDbContext> baseDeDatos;
        private Avisos avisos;
        public Usuarios(IDbContextFactory<AppDbContext> baseDeDatos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.avisos = avisos;
        }

        public async Task<Cuenta?> BuscarAsync(int? id, string? correo, CancellationToken ct, bool incluirDeshabilitadas = false)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            IQueryable<Cuenta> consulta =
                from usuario in contexto.Usuarios.AsNoTracking()join persona in contexto.Personas on usuario.IdPersona equals persona.IdPersona
                join rol in contexto.Roles on usuario.IdRol equals rol.IdRol
                where (incluirDeshabilitadas || usuario.Estado == "Habilitada") && rol.Activo && ((id != null && usuario.IdUsuario == id) || (correo != null && usuario.CorreoInstitucional == correo))select new Cuenta
                {
                    Id = usuario.IdUsuario,
                    Nombre = usuario.NombreMostrar ?? persona.Nombres + " " + persona.Apellidos,
                    Correo = usuario.CorreoInstitucional,
                    Rol = rol.Nombre,
                    Hash = usuario.HashContrasena,
                    Avatar = usuario.Avatar,
                    Tema = usuario.Tema,
                    VersionRol = usuario.VersionRol,
                    FotoVersion = usuario.FotoVersion.HasValue ? usuario.FotoVersion.Value.ToString() : null,
                    IdCarrera = usuario.IdCarrera,
                    IdPlan = usuario.IdPlan,
                    Habilitada = usuario.Estado == "Habilitada"
                };
            return await consulta.FirstOrDefaultAsync(ct);
        }

        public async Task<int> RegistrarLectorAsync(string nombres, string apellidos, string correo, string hash, CancellationToken ct, int? idCarrera = null, int? idPlan = null)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction transaccion = await contexto.Database.BeginTransactionAsync(ct);
            string nombreRol = idCarrera.HasValue ? "Estudiante" : "Lector";
            int rol = await contexto.Roles.Where(r => r.Nombre == nombreRol && r.Activo).Select(r => r.IdRol).FirstOrDefaultAsync(ct);
            if (rol == 0)
            {
                throw new ErrorApi(409, "El registro de lectores todavía no está disponible.");
            }

            await ValidarCarrera(contexto, transaccion, idCarrera, ct);
            await ValidarPlan(contexto, transaccion, idCarrera, idPlan, ct);
            try
            {
                PersonaModelo persona = new PersonaModelo();
                persona.Nombres = nombres;
                persona.Apellidos = apellidos;
                contexto.Personas.Add(persona);
                await contexto.SaveChangesAsync(ct);
                UsuarioModelo usuario = new UsuarioModelo();
                usuario.IdPersona = persona.IdPersona;
                usuario.CorreoInstitucional = correo;
                usuario.HashContrasena = hash;
                usuario.IdRol = rol;
                usuario.IdCarrera = idCarrera;
                usuario.IdPlan = idPlan;
                contexto.Usuarios.Add(usuario);
                await contexto.SaveChangesAsync(ct);
                await avisos.Cuenta(contexto, transaccion, usuario.IdUsuario, "REGISTRO_CUENTA", ct);
                await transaccion.CommitAsync(ct);
                return usuario.IdUsuario;
            }
            catch (DbUpdateException e)when (e.InnerException is PostgresException pg && pg.ConstraintName == "uq_usuarios_correo_institucional")
            {
                throw new ErrorApi(409, "Ya existe una cuenta con ese correo.");
            }
        }

        internal static async Task ValidarCarrera(AppDbContext contexto, IDbContextTransaction? transaccion, int? carrera, CancellationToken ct)
        {
            if (carrera is null)
            {
                return;
            }

            IQueryable<int> consulta =
                from c in contexto.Carreras
                join departamento in contexto.Departamentos on c.IdDepartamento equals departamento.IdDepartamento
                where c.IdCarrera == carrera && c.Activa && departamento.Activa
                select c.IdCarrera;
            if (!await consulta.AnyAsync(ct))
            {
                throw new ErrorApi(400, "Selecciona una carrera activa.");
            }
        }

        internal static async Task ValidarPlan(AppDbContext contexto, IDbContextTransaction? transaccion, int? carrera, int? plan, CancellationToken ct)
        {
            if (plan is null)
            {
                return;
            }

            if (!await contexto.PlanesEstudio.AnyAsync(p => p.IdPlan == plan && p.IdCarrera == carrera && p.Activa, ct))
            {
                throw new ErrorApi(400, "El plan seleccionado debe pertenecer a tu carrera.");
            }
        }

        public async Task ActualizarPerfilAsync(int id, string? nombre, string? avatar, string? tema, CancellationToken ct, int? idCarrera = null, int? idPlan = null)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            await using IDbContextTransaction transaccion = await contexto.Database.BeginTransactionAsync(ct);
            await ValidarCarrera(contexto, transaccion, idCarrera, ct);
            await ValidarPlan(contexto, transaccion, idCarrera, idPlan, ct);
            List<UsuarioModelo> cuentas = await contexto.Usuarios.FromSqlRaw("SELECT * FROM public.usuarios WHERE id_usuario=@id AND estado='Habilitada' FOR UPDATE", Comandos.Parametros(("id", id))).ToListAsync(ct);
            UsuarioModelo? usuario = cuentas.FirstOrDefault();
            if (usuario is null)
            {
                throw new ErrorApi(409, "La cuenta ya no está habilitada.");
            }

            if (nombre is not null)
            {
                usuario.NombreMostrar = nombre;
            }

            if (avatar is not null)
            {
                usuario.Avatar = avatar;
            }

            if (tema is not null)
            {
                usuario.Tema = tema;
            }

            if (idCarrera.HasValue)
            {
                usuario.IdCarrera = idCarrera;
                usuario.IdPlan = idPlan;
                bool lector = await contexto.Roles.AnyAsync(r => r.IdRol == usuario.IdRol && r.Nombre == "Lector", ct);
                if (lector)
                {
                    int rol = await contexto.Roles.Where(r => r.Nombre == "Estudiante" && r.Activo).Select(r => r.IdRol).FirstOrDefaultAsync(ct);
                    if (rol == 0)
                    {
                        throw new ErrorApi(409, "El rol estudiantil no está disponible.");
                    }

                    usuario.IdRol = rol;
                    usuario.VersionRol++;
                }
            }

            await contexto.SaveChangesAsync(ct);
            await transaccion.CommitAsync(ct);
        }

        public async Task GuardarFoto(int id, byte[]? foto, string? tipo, CancellationToken ct)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            Guid? version = foto is null ? null : Guid.NewGuid();
            int filas = await contexto.Usuarios.Where(u => u.IdUsuario == id && u.Estado == "Habilitada").ExecuteUpdateAsync(cambios => cambios.SetProperty(u => u.FotoPerfil, foto).SetProperty(u => u.FotoTipo, tipo).SetProperty(u => u.FotoVersion, version), ct);
            if (filas != 1)
            {
                throw new ErrorApi(409, "La cuenta ya no está habilitada.");
            }
        }

        public async Task<(byte[] Datos, string Tipo)?> LeerFoto(int id, CancellationToken ct)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            var foto = await contexto.Usuarios.AsNoTracking().Where(u => u.IdUsuario == id && u.FotoPerfil != null).Select(u => new { Datos = u.FotoPerfil, Tipo = u.FotoTipo }).FirstOrDefaultAsync(ct);
            if (foto is null)
            {
                return null;
            }

            return (foto.Datos!, foto.Tipo!);
        }

        public async Task CambiarContrasenaAsync(int id, string anterior, string nuevo, CancellationToken ct)
        {
            await using AppDbContext contexto = await baseDeDatos.CreateDbContextAsync(ct);
            int filas = await contexto.Usuarios.Where(u => u.IdUsuario == id && u.HashContrasena == anterior && u.Estado == "Habilitada").ExecuteUpdateAsync(cambios => cambios.SetProperty(u => u.HashContrasena, nuevo), ct);
            if (filas != 1)
            {
                throw new ErrorApi(409, "La cuenta cambió. Vuelve a iniciar sesión.");
            }
        }
    }

    public class Cuenta
    {
        public bool Habilitada { get; init; }
        public int Id { get; init; }
        public string Nombre { get; init; } = "";
        public string Correo { get; init; } = "";
        public string Rol { get; init; } = "";
        public string Hash { get; init; } = "";
        public string Avatar { get; init; } = "turquesa";
        public string Tema { get; init; } = "sistema";
        public int VersionRol { get; init; }
        public string? FotoVersion { get; init; }
        public int? IdCarrera { get; init; }
        public int? IdPlan { get; init; }

        public string VersionSesion
        {
            get
            {
                string valor = VersionRol == 0 ? Hash : FormattableString.Invariant($"{Hash}:{VersionRol}");
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(valor)));
            }
        }
    }
}
