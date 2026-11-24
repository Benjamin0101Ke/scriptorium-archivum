using UglyToad.PdfPig;

namespace APIScriptoriumArchivum.Datos
{
    public class ArchivoPdf
    {
        private string carpeta;

        public ArchivoPdf(IConfiguration configuracion, IWebHostEnvironment entorno)
        {
            carpeta = Path.GetFullPath(configuracion["Storage:Ruta"] ??
                Path.Combine(entorno.ContentRootPath, "storage", "trabajos"));
        }

        public string? Ubicar(string? clave)
        {
            if (clave is null || clave.Length != 36 || !clave.EndsWith(".pdf", StringComparison.Ordinal) ||
                !Guid.TryParseExact(clave[..32], "N", out _))
            {
                return null;
            }
            string archivo = Path.Combine(carpeta, clave);
            return File.Exists(archivo) ? archivo : null;
        }

        public async Task<string> Guardar(IFormFile archivo, CancellationToken ct)
        {
            await using Stream entrada = archivo.OpenReadStream();
            byte[] encabezado = new byte[5];
            await entrada.ReadExactlyAsync(encabezado, ct);
            if (!encabezado.AsSpan().SequenceEqual("%PDF-"u8))
            {
                throw new InvalidDataException("El archivo debe ser un PDF.");
            }

            Directory.CreateDirectory(carpeta);
            string clave = Guid.NewGuid().ToString("N") + ".pdf";
            string ruta = Path.Combine(carpeta, clave);
            try
            {
                await using (FileStream salida = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write))
                {
                    await salida.WriteAsync(encabezado, ct);
                    await entrada.CopyToAsync(salida, ct);
                }

                ValidarDocumento(ruta);
                return clave;
            }
            catch
            {
                File.Delete(ruta);
                throw;
            }
        }

        private static void ValidarDocumento(string ruta)
        {
            try
            {
                using PdfDocument documento = PdfDocument.Open(ruta, new ParsingOptions { UseLenientParsing = false });
                if (documento.NumberOfPages == 0)
                {
                    throw new InvalidDataException("El PDF no tiene páginas.");
                }

                documento.GetPage(1);
            }
            catch (Exception error) when (error is not (IOException or UnauthorizedAccessException or OutOfMemoryException))
            {
                throw new InvalidDataException("El archivo no contiene un PDF válido que se pueda abrir.", error);
            }
        }

        public void QuitarTemporal(string clave)
        {
            string? archivo = Ubicar(clave);
            if (archivo is not null)
            {
                File.Delete(archivo);
            }
        }
    }
}
