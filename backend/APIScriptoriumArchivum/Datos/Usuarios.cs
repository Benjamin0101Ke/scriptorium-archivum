using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Usuarios
    {
        private NpgsqlDataSource baseDeDatos;
        private Avisos avisos;

        public Usuarios(NpgsqlDataSource baseDeDatos, Avisos avisos)
        {
            this.baseDeDatos = baseDeDatos;
            this.avisos = avisos;
        }

        public async Task<Cuenta?> BuscarAsync(int? id, string? correo, CancellationToken ct, bool incluirDeshabilitadas = false)
        {
            const string sql = """
                SELECT u.id_usuario, coalesce(u.nombre_mostrar, concat_ws(' ', p.nombres, p.apellidos)),
                       u.correo_institucional, r.nombre, u.hash_contrasena, u.avatar, u.tema, u.version_rol,
                       u.foto_version::text, u.id_carrera, u.id_plan, u.estado = 'Habilitada'
                FROM public.usuarios u
                JOIN public.personas p ON p.id_persona = u.id_persona
                JOIN public.roles r ON r.id_rol = u.id_rol
                WHERE (@incluir OR u.estado = 'Habilitada') AND r.activo
                  AND ((@id::integer IS NOT NULL AND u.id_usuario = @id::integer)
                    OR (@correo::text IS NOT NULL AND u.correo_institucional = @correo::text))
                """;
            await using NpgsqlConnection conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using NpgsqlCommand comando = Comandos.Crear(conexion, null, sql, ("id", id), ("correo", correo), ("incluir", incluirDeshabilitadas));
            await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(ct);
            if (!await lector.ReadAsync(ct))
            {
                return null;
            }

            return new Cuenta
            {
                Id = lector.GetInt32(0),
                Nombre = lector.GetString(1),
                Correo = lector.GetString(2),
                Rol = lector.GetString(3),
                Hash = lector.GetString(4),
                Avatar = lector.GetString(5),
                Tema = lector.GetString(6),
                VersionRol = lector.GetInt32(7),
                FotoVersion = lector.IsDBNull(8) ? null : lector.GetString(8),
                IdCarrera = lector.IsDBNull(9) ? null : lector.GetInt32(9),
                IdPlan = lector.IsDBNull(10) ? null : lector.GetInt32(10),
                Habilitada = lector.GetBoolean(11)
            };
        }

        public async Task<int> RegistrarLectorAsync(string nombres, string apellidos, string correo,
            string hash, CancellationToken ct, int? idCarrera = null, int? idPlan = null)
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var tr = await conexion.BeginTransactionAsync(ct);
            int rol = await Comandos.Valor<int>(conexion, tr,
                "SELECT id_rol FROM public.roles WHERE nombre = @rol AND activo", ct,
                ("rol", idCarrera.HasValue ? "Estudiante" : "Lector"));
            if (rol == 0) throw new ErrorApi(409, "El registro de lectores todavía no está disponible.");
            await ValidarCarrera(conexion, tr, idCarrera, ct);
            await ValidarPlan(conexion, tr, idCarrera, idPlan, ct);

            try
            {
                int id = await Comandos.Valor<int>(conexion, tr, """
                    WITH persona AS (
                        INSERT INTO public.personas (nombres, apellidos) VALUES (@nombres, @apellidos)
                        RETURNING id_persona
                    )
                    INSERT INTO public.usuarios (id_persona, correo_institucional, hash_contrasena, id_rol, id_carrera, id_plan)
                    SELECT id_persona, @correo, @hash, @rol, @carrera, @plan FROM persona RETURNING id_usuario
                    """, ct, ("nombres", nombres), ("apellidos", apellidos), ("correo", correo),
                    ("hash", hash), ("rol", rol), ("carrera", idCarrera), ("plan", idPlan));
                await avisos.Cuenta(conexion, tr, id, "REGISTRO_CUENTA", ct);
                await tr.CommitAsync(ct);
                return id;
            }
            catch (PostgresException e) when (e.ConstraintName == "uq_usuarios_correo_institucional")
            {
                throw new ErrorApi(409, "Ya existe una cuenta con ese correo.");
            }
        }

        internal static async Task ValidarCarrera(NpgsqlConnection c, NpgsqlTransaction? tr, int? carrera, CancellationToken ct)
        {
            if (carrera is null) return;
            bool valida = await Comandos.Valor<bool>(c, tr, """
                SELECT EXISTS(SELECT 1 FROM public.carreras c JOIN public.departamentos a USING(id_departamento)
                    WHERE c.id_carrera=@id AND c.activa AND a.activa)
                """, ct, ("id", carrera));
            if (!valida) throw new ErrorApi(400, "Selecciona una carrera activa.");
        }

        internal static async Task ValidarPlan(NpgsqlConnection c, NpgsqlTransaction? tr, int? carrera, int? plan, CancellationToken ct)
        {
            if (plan is null) return;
            bool valido = await Comandos.Valor<bool>(c, tr, """
                SELECT EXISTS(SELECT 1 FROM public.planes_estudio WHERE id_plan=@plan AND id_carrera=@carrera AND activa)
                """, ct, ("plan", plan), ("carrera", carrera));
            if (!valido) throw new ErrorApi(400, "El plan seleccionado debe pertenecer a tu carrera.");
        }

        public async Task ActualizarPerfilAsync(int id, string? nombre, string? avatar, string? tema, CancellationToken ct, int? idCarrera = null, int? idPlan = null)
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var tr = await conexion.BeginTransactionAsync(ct);
            await ValidarCarrera(conexion, tr, idCarrera, ct);
            await ValidarPlan(conexion, tr, idCarrera, idPlan, ct);
            await using var comando = Comandos.Crear(conexion, tr, """
                UPDATE public.usuarios SET nombre_mostrar = coalesce(@nombre::text, nombre_mostrar),
                    avatar = coalesce(@avatar::text, avatar), tema = coalesce(@tema::text, tema),
                    id_carrera=coalesce(@carrera::int,id_carrera),
                    id_plan=CASE WHEN @carrera::int IS NOT NULL THEN @plan::int ELSE id_plan END,
                    version_rol=version_rol + CASE WHEN @carrera::int IS NOT NULL
                        AND id_rol=(SELECT id_rol FROM public.roles WHERE nombre='Lector') THEN 1 ELSE 0 END,
                    id_rol=CASE WHEN @carrera::int IS NOT NULL AND id_rol=(SELECT id_rol FROM public.roles WHERE nombre='Lector')
                        THEN (SELECT id_rol FROM public.roles WHERE nombre='Estudiante' AND activo) ELSE id_rol END
                WHERE id_usuario = @id AND estado = 'Habilitada'
                """, ("nombre", nombre), ("avatar", avatar), ("tema", tema), ("id", id), ("carrera", idCarrera), ("plan", idPlan));
            if (await comando.ExecuteNonQueryAsync(ct) != 1)
                throw new ErrorApi(409, "La cuenta ya no está habilitada.");
            await tr.CommitAsync(ct);
        }

        public async Task GuardarFoto(int id, byte[]? foto, string? tipo, CancellationToken ct)
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var comando = Comandos.Crear(conexion, null, """
                UPDATE public.usuarios SET foto_perfil=@foto::bytea, foto_tipo=@tipo::text,
                    foto_version=@version::uuid WHERE id_usuario=@id AND estado='Habilitada'
                """, ("id", id), ("foto", foto), ("tipo", tipo), ("version", foto is null ? (Guid?)null : Guid.NewGuid()));
            if (await comando.ExecuteNonQueryAsync(ct) != 1)
                throw new ErrorApi(409, "La cuenta ya no está habilitada.");
        }

        public async Task<(byte[] Datos, string Tipo)?> LeerFoto(int id, CancellationToken ct)
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var comando = Comandos.Crear(conexion, null,
                "SELECT foto_perfil, foto_tipo FROM public.usuarios WHERE id_usuario=@id AND foto_perfil IS NOT NULL",
                ("id", id));
            await using var lector = await comando.ExecuteReaderAsync(ct);
            return await lector.ReadAsync(ct) ? (lector.GetFieldValue<byte[]>(0), lector.GetString(1)) : null;
        }

        public async Task CambiarContrasenaAsync(int id, string anterior, string nuevo, CancellationToken ct)
        {
            await using var conexion = await baseDeDatos.OpenConnectionAsync(ct);
            await using var comando = Comandos.Crear(conexion, null, """
                UPDATE public.usuarios SET hash_contrasena = @nuevo
                WHERE id_usuario = @id AND hash_contrasena = @anterior AND estado = 'Habilitada'
                """, ("nuevo", nuevo), ("id", id), ("anterior", anterior));
            if (await comando.ExecuteNonQueryAsync(ct) != 1)
                throw new ErrorApi(409, "La cuenta cambió. Vuelve a iniciar sesión.");
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

        public string VersionSesion => Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
                VersionRol == 0 ? Hash : FormattableString.Invariant($"{Hash}:{VersionRol}"))));
    }
}
