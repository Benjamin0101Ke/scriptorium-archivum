using APIScriptoriumArchivum.Dtos;
using Npgsql;

namespace APIScriptoriumArchivum.Datos
{
    public class Catalogos
    {
        private readonly NpgsqlDataSource baseDeDatos;

        public Catalogos(NpgsqlDataSource baseDeDatos)
        {
            this.baseDeDatos = baseDeDatos;
        }

        public async Task<List<CarreraDto>> ListarCarrerasAsync(CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT id_carrera, nombre
                FROM public.carreras
                WHERE activa
                ORDER BY nombre, id_carrera
                """;

            List<CarreraDto> carreras = new List<CarreraDto>();

            await using NpgsqlCommand comando = baseDeDatos.CreateCommand(consulta);
            await using NpgsqlDataReader filas = await comando.ExecuteReaderAsync(cancellationToken);

            while (await filas.ReadAsync(cancellationToken))
            {
                carreras.Add(new CarreraDto(filas.GetInt32(0), filas.GetString(1)));
            }

            return carreras;
        }

        public async Task<List<TipoDeTrabajoDto>> ListarTiposDeTrabajoAsync(CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT id_tipo_trabajo, nombre
                FROM public.tipos_trabajo
                WHERE activa
                ORDER BY nombre, id_tipo_trabajo
                """;

            List<TipoDeTrabajoDto> tiposDeTrabajo = new List<TipoDeTrabajoDto>();

            await using NpgsqlCommand comando = baseDeDatos.CreateCommand(consulta);
            await using NpgsqlDataReader filas = await comando.ExecuteReaderAsync(cancellationToken);

            while (await filas.ReadAsync(cancellationToken))
            {
                tiposDeTrabajo.Add(new TipoDeTrabajoDto(filas.GetInt32(0), filas.GetString(1)));
            }

            return tiposDeTrabajo;
        }

        public async Task<List<AreaTematicaDto>> ListarAreasTematicasAsync(CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT id_area, nombre
                FROM public.areas_tematicas
                WHERE activa
                ORDER BY nombre, id_area
                """;

            List<AreaTematicaDto> areasTematicas = new List<AreaTematicaDto>();

            await using NpgsqlCommand comando = baseDeDatos.CreateCommand(consulta);
            await using NpgsqlDataReader filas = await comando.ExecuteReaderAsync(cancellationToken);

            while (await filas.ReadAsync(cancellationToken))
            {
                areasTematicas.Add(new AreaTematicaDto(filas.GetInt32(0), filas.GetString(1)));
            }

            return areasTematicas;
        }
    }
}
