using System.Text.Json;
using Npgsql;
using NpgsqlTypes;

namespace APIScriptoriumArchivum.Datos
{
    internal static class Comandos
    {
        public static NpgsqlCommand Crear(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql,
            params (string Nombre, object? Valor)[] parametros)
        {
            NpgsqlCommand comando = new NpgsqlCommand(sql, conexion, transaccion);
            foreach ((string nombre, object? valor) in parametros)
            {
                if (valor is null)
                {
                    comando.Parameters.AddWithValue(nombre, NpgsqlDbType.Unknown, DBNull.Value);
                }
                else
                {
                    comando.Parameters.AddWithValue(nombre, valor);
                }
            }
            return comando;
        }

        public static async Task Ejecutar(NpgsqlConnection conexion, NpgsqlTransaction transaccion, string sql,
            CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            await using NpgsqlCommand comando = Crear(conexion, transaccion, sql, parametros);
            await comando.ExecuteNonQueryAsync(ct);
        }

        public static async Task<T?> Valor<T>(NpgsqlConnection conexion, NpgsqlTransaction? transaccion, string sql,
            CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            await using NpgsqlCommand comando = Crear(conexion, transaccion, sql, parametros);
            object? valor = await comando.ExecuteScalarAsync(ct);
            return valor is T resultado ? resultado : default;
        }

        public static async Task<List<JsonElement>> FilasJson(NpgsqlConnection conexion, string sql,
            CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            return await FilasJson(conexion, null, sql, ct, parametros);
        }

        public static async Task<List<JsonElement>> FilasJson(NpgsqlConnection conexion, NpgsqlTransaction? transaccion,
            string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            List<JsonElement> filas = new List<JsonElement>();
            await using NpgsqlCommand comando = Crear(conexion, transaccion, sql, parametros);
            await using NpgsqlDataReader lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct))
            {
                using JsonDocument fila = JsonDocument.Parse(lector.GetString(0));
                filas.Add(fila.RootElement.Clone());
            }
            return filas;
        }
    }
}
