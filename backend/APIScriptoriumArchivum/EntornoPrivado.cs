using System.Text;
using Npgsql;

namespace APIScriptoriumArchivum;

// Lee la configuracion privada para ejecutar la API fuera de Docker
public static class EntornoPrivado
{
    // Carga el archivo local sin publicar sus valores ni cambiar el entorno del equipo
    public static void Cargar(WebApplicationBuilder builder, string[] args)
    {
        string? seleccion = builder.Configuration["ArchivoEntorno"];
        string ruta = Path.Combine(builder.Environment.ContentRootPath, ".env");
        if (!string.IsNullOrWhiteSpace(seleccion))
        {
            ruta = Path.GetFullPath(seleccion, builder.Environment.ContentRootPath);
            if (!File.Exists(ruta))
            {
                throw new InvalidOperationException("No se encontro el archivo privado indicado en ArchivoEntorno");
            }
        }
        else if (!File.Exists(ruta) && builder.Environment.IsDevelopment())
        {
            ruta = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".env"));
        }

        if (!File.Exists(ruta))
        {
            return;
        }

        Dictionary<string, string> valores = Leer(ruta);
        Dictionary<string, string?> ajustes = new Dictionary<string, string?>();
        Dictionary<string, string> nombres = new Dictionary<string, string>
        {
            { "SMTP_HOST", "Correo:Host" },
            { "SMTP_PORT", "Correo:Puerto" },
            { "SMTP_TLS", "Correo:Tls" },
            { "SMTP_FROM", "Correo:Remitente" },
            { "SMTP_USER", "Correo:Usuario" },
            { "SMTP_PASSWORD", "Correo:Password" },
            { "SMTP_AVISOS", "Correo:Avisos" },
            { "SMTP_AVISOS_ACCESO", "Correo:AvisosAcceso" },
            { "SMTP_HABILITADO", "Correo:Habilitado" }
        };
        foreach (KeyValuePair<string, string> nombre in nombres)
        {
            if (valores.TryGetValue(nombre.Key, out string? valor))
            {
                ajustes[nombre.Value] = valor;
            }
        }

        // Completa la cadena local con la clave privada y el puerto publicado
        NpgsqlConnectionStringBuilder cadena = new NpgsqlConnectionStringBuilder(
            builder.Configuration.GetConnectionString("Scriptorium"));
        if (valores.TryGetValue("POSTGRES_PASSWORD", out string? password))
        {
            cadena.Password = password;
        }
        if ((cadena.Host == "localhost" || cadena.Host == "127.0.0.1") &&
            valores.TryGetValue("POSTGRES_PORT", out string? puerto))
        {
            if (!int.TryParse(puerto, out int numero) || numero < 1 || numero > 65535)
            {
                throw new InvalidOperationException("POSTGRES_PORT debe ser un puerto valido");
            }
            cadena.Port = numero;
        }
        ajustes["ConnectionStrings:Scriptorium"] = cadena.ConnectionString;
        builder.Configuration.AddInMemoryCollection(ajustes);

        // Las variables del despliegue y los argumentos tienen prioridad
        builder.Configuration.AddEnvironmentVariables();
        builder.Configuration.AddCommandLine(args);
    }

    // Obtiene la cadena de conexion completada con la configuracion privada
    public static string CadenaConexion(IConfiguration configuracion)
    {
        string? valor = configuracion.GetConnectionString("Scriptorium");
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException("Falta configurar ConnectionStrings:Scriptorium");
        }
        return valor;
    }

    // Lee pares de nombre y valor y conserva los simbolos de las claves
    private static Dictionary<string, string> Leer(string ruta)
    {
        Dictionary<string, string> valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string linea in File.ReadAllLines(ruta, new UTF8Encoding(false, true)))
        {
            string texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('#'))
            {
                continue;
            }
            int separador = texto.IndexOf('=');
            if (separador <= 0)
            {
                throw new InvalidOperationException("El archivo privado debe contener pares NOMBRE=valor");
            }
            string nombre = texto[..separador].Trim();
            string valor = texto[(separador + 1)..].Trim();
            if (valor.StartsWith('"') || valor.StartsWith('\''))
            {
                if (valor.Length < 2 || valor[^1] != valor[0])
                {
                    throw new InvalidOperationException("Hay comillas sin cerrar en el archivo privado");
                }
                valor = valor[1..^1];
            }
            if (!valores.TryAdd(nombre, valor))
            {
                throw new InvalidOperationException("Hay nombres repetidos en el archivo privado");
            }
        }
        return valores;
    }
}
