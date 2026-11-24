using System.Collections.Concurrent;
using System.Text.Json;

namespace APIScriptoriumArchivum.Datos;

public sealed class CargasArchivos : BackgroundService
{
    public const int ParteBytes = 8 * 1024 * 1024;
    public const long MaximoBytes = 6L * 1024 * 1024 * 1024;
    private readonly string carpeta;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> candados = new();

    public CargasArchivos(IConfiguration configuracion, IWebHostEnvironment entorno)
    {
        carpeta = Path.Combine(Path.GetFullPath(configuracion["Storage:Ruta"] ??
            Path.Combine(entorno.ContentRootPath, "storage", "trabajos")), ".cargas");
        Directory.CreateDirectory(carpeta);
    }

    private string Ruta(Guid id) => Path.Combine(carpeta, id.ToString("N"));
    private SemaphoreSlim Candado(Guid id) => candados.GetOrAdd(id, _ => new(1, 1));
    private sealed record Estado(int Usuario, string Nombre, long Total, long Recibidos);

    private async Task<Estado> Leer(Guid id, int usuario, CancellationToken ct)
    {
        string ruta = Path.Combine(Ruta(id), "estado.json");
        if (!File.Exists(ruta)) throw new ErrorApi(404, "La carga no existe o venció. Selecciona el archivo de nuevo.");
        var estado = JsonSerializer.Deserialize<Estado>(await File.ReadAllTextAsync(ruta, ct))!;
        if (estado.Usuario != usuario) throw new ErrorApi(403, "Esta carga pertenece a otra cuenta.");
        return estado;
    }

    private async Task Guardar(Guid id, Estado estado, CancellationToken ct)
    {
        string ruta = Path.Combine(Ruta(id), "estado.json");
        await File.WriteAllTextAsync(ruta + ".tmp", JsonSerializer.Serialize(estado), ct);
        File.Move(ruta + ".tmp", ruta, true);
    }

    public async Task<Guid> Crear(int usuario, string? nombre, long total, CancellationToken ct)
    {
        nombre = Path.GetFileName((nombre ?? "").Replace('\\', '/'));
        if (total > MaximoBytes) throw new ErrorApi(400, "El archivo supera el máximo de 6 GB.");
        if (total <= 0 || nombre.Length is 0 or > 255 || nombre.Any(char.IsControl))
            throw new ErrorApi(400, "Selecciona un archivo con nombre y contenido válidos.");
        Guid id = Guid.NewGuid();
        var candado = Candado(id);
        await candado.WaitAsync(ct);
        try
        {
            Directory.CreateDirectory(Ruta(id));
            await Guardar(id, new(usuario, nombre, total, 0), ct);
        }
        finally { candado.Release(); }
        return id;
    }

    public async Task<long> Agregar(Guid id, int usuario, long parte, Stream entrada, long? longitud, CancellationToken ct)
    {
        var candado = Candado(id);
        await candado.WaitAsync(ct);
        try
        {
            var estado = await Leer(id, usuario, ct);
            if (parte < 0 || parte > estado.Total / ParteBytes)
                throw new ErrorApi(400, "La parte indicada no es válida.");
            long inicio = checked(parte * ParteBytes);
            long esperados = Math.Min(ParteBytes, estado.Total - inicio);
            if (esperados <= 0 || longitud != esperados)
                throw new ErrorApi(400, "El tamaño de la parte no coincide con el archivo.");
            if (inicio < estado.Recibidos) return estado.Recibidos;
            if (inicio != estado.Recibidos) throw new ErrorApi(409, "Envía las partes del archivo en orden.");
            await using var salida = new FileStream(Path.Combine(Ruta(id), "contenido"), FileMode.OpenOrCreate,
                FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            salida.SetLength(inicio);
            salida.Position = inicio;
            byte[] buffer = new byte[65536];
            long escritos = 0;
            try
            {
                while (escritos < esperados)
                {
                    int leidos = await entrada.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, esperados - escritos)), ct);
                    if (leidos == 0) throw new ErrorApi(400, "La transferencia se interrumpió. Vuelve a intentar.");
                    await salida.WriteAsync(buffer.AsMemory(0, leidos), ct);
                    escritos += leidos;
                }
                await salida.FlushAsync(ct);
                await Guardar(id, estado with { Recibidos = inicio + escritos }, ct);
                return inicio + escritos;
            }
            catch { salida.SetLength(inicio); throw; }
        }
        finally { candado.Release(); }
    }

    public async Task<Recibido> Resolver(IFormFile? archivo, string? cargaId, int usuario, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cargaId))
        {
            if (archivo?.Length > MaximoBytes) throw new ErrorApi(400, "El archivo supera el máximo de 6 GB.");
            return new(archivo, null, null, null);
        }
        if (archivo is not null || !Guid.TryParse(cargaId, out Guid id))
            throw new ErrorApi(400, "Indica una sola carga de archivo válida.");
        var candado = Candado(id);
        await candado.WaitAsync(ct);
        try
        {
            var estado = await Leer(id, usuario, ct);
            if (estado.Total != estado.Recibidos) throw new ErrorApi(409, "El archivo todavía no terminó de subir.");
            var entrada = new FileStream(Path.Combine(Ruta(id), "contenido"), FileMode.Open, FileAccess.Read,
                FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            return new(new FormFile(entrada, 0, estado.Total, "archivo", estado.Nombre), entrada, Ruta(id), candado,
                () => candados.TryRemove(id, out _));
        }
        catch { candado.Release(); throw; }
    }

    public async Task Borrar(Guid id, int usuario, CancellationToken ct)
    {
        var candado = Candado(id);
        await candado.WaitAsync(ct);
        try
        {
            if (!Directory.Exists(Ruta(id))) { candados.TryRemove(id, out _); return; }
            await Leer(id, usuario, ct);
            Directory.Delete(Ruta(id), true);
            candados.TryRemove(id, out _);
        }
        finally { candado.Release(); }
    }

    public sealed class Recibido(IFormFile? archivo, Stream? entrada, string? ruta, SemaphoreSlim? candado,
        Action? alTerminar = null) : IAsyncDisposable
    {
        public IFormFile? Archivo => archivo;
        public async ValueTask DisposeAsync()
        {
            try
            {
                if (entrada is not null) await entrada.DisposeAsync();
                if (ruta is not null && Directory.Exists(ruta)) Directory.Delete(ruta, true);
            }
            finally { candado?.Release(); alTerminar?.Invoke(); }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var reloj = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            foreach (string ruta in Directory.EnumerateDirectories(carpeta))
            {
                if (!Guid.TryParseExact(Path.GetFileName(ruta), "N", out Guid id)) continue;
                var candado = Candado(id);
                if (!await candado.WaitAsync(0, stoppingToken)) continue;
                try
                {
                    string estado = Path.Combine(ruta, "estado.json");
                    if (File.GetLastWriteTimeUtc(estado) < DateTime.UtcNow.AddDays(-1))
                    {
                        Directory.Delete(ruta, true);
                        candados.TryRemove(id, out _);
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                finally { candado.Release(); }
            }
        } while (await reloj.WaitForNextTickAsync(stoppingToken));
    }
}
