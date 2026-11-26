using System.Text.Json;
using APIScriptoriumArchivum.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;

namespace APIScriptoriumArchivum.Datos
{
    public static class Comandos
    {
        public static object[] Parametros(params (string Nombre, object? Valor)[] valores)
        {
            List<object> parametros = new List<object>();
            foreach ((string nombre, object? valor)in valores)
            {
                NpgsqlParameter parametro;
                if (valor is null)
                {
                    parametro = new NpgsqlParameter(nombre, NpgsqlDbType.Unknown);
                    parametro.Value = DBNull.Value;
                }
                else
                {
                    parametro = new NpgsqlParameter(nombre, valor);
                }

                parametros.Add(parametro);
            }

            return parametros.ToArray();
        }

        private static void ValidarTransaccion(AppDbContext contexto, IDbContextTransaction? transaccion)
        {
            if (transaccion is not null && contexto.Database.CurrentTransaction != transaccion)
            {
                throw new InvalidOperationException("La transaccion no corresponde al contexto");
            }
        }

        public static async Task<int> Ejecutar(AppDbContext contexto, IDbContextTransaction? transaccion, string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            ValidarTransaccion(contexto, transaccion);
            return await contexto.Database.ExecuteSqlRawAsync(sql, Parametros(parametros), ct);
        }

        public static async Task<List<T>> Filas<T>(AppDbContext contexto, string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            return await contexto.Database.SqlQueryRaw<T>(sql, Parametros(parametros)).ToListAsync(ct);
        }

        public static async Task<T?> Valor<T>(AppDbContext contexto, IDbContextTransaction? transaccion, string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            ValidarTransaccion(contexto, transaccion);
            object? valor;
            if (typeof(T) == typeof(int))
            {
                List<int?> filas = await Filas<int?>(contexto, sql, ct, parametros);
                valor = filas.FirstOrDefault() ?? 0;
            }
            else if (typeof(T) == typeof(long))
            {
                List<long?> filas = await Filas<long?>(contexto, sql, ct, parametros);
                valor = filas.FirstOrDefault() ?? 0L;
            }
            else if (typeof(T) == typeof(bool))
            {
                List<bool?> filas = await Filas<bool?>(contexto, sql, ct, parametros);
                valor = filas.FirstOrDefault() ?? false;
            }
            else
            {
                List<T> filas = await Filas<T>(contexto, sql, ct, parametros);
                return filas.FirstOrDefault();
            }

            return (T)valor;
        }

        public static async Task<List<JsonElement>> FilasJson(AppDbContext contexto, string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            return await FilasJson(contexto, null, sql, ct, parametros);
        }

        public static async Task<List<JsonElement>> FilasJson(AppDbContext contexto, IDbContextTransaction? transaccion, string sql, CancellationToken ct, params (string Nombre, object? Valor)[] parametros)
        {
            ValidarTransaccion(contexto, transaccion);
            List<string> filas = await Filas<string>(contexto, sql, ct, parametros);
            List<JsonElement> resultado = new List<JsonElement>();
            foreach (string texto in filas)
            {
                using JsonDocument fila = JsonDocument.Parse(texto);
                resultado.Add(fila.RootElement.Clone());
            }

            return resultado;
        }
    }
}
