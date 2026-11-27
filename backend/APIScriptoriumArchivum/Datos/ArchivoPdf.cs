using UglyToad.PdfPig;

namespace APIScriptoriumArchivum.Datos
{
    // Valida los PDF recibidos y administra sus archivos en el almacenamiento
    // El controlador recibe la peticion de Angular y entrega aqui el archivo
    public class ArchivoPdf
    {
        // Ruta absoluta de la carpeta donde se guardan los PDF
        private string carpeta;
        // Recibe la configuracion y la ubicacion del proyecto para resolver la carpeta
        public ArchivoPdf(IConfiguration configuracion, IWebHostEnvironment entorno)
        {
            // Usa Storage Ruta y si falta utiliza storage trabajos dentro del proyecto
            carpeta = Path.GetFullPath(configuracion["Storage:Ruta"] ?? Path.Combine(entorno.ContentRootPath, "storage", "trabajos"));
        }

        // Busca un PDF por su clave y devuelve su ruta o null si no se encuentra
        public string? Ubicar(string? clave)
        {
            // Solo acepta nombres con el mismo formato que genera Guardar
            if (clave is null || clave.Length != 36 || !clave.EndsWith(".pdf", StringComparison.Ordinal) || !Guid.TryParseExact(clave[..32], "N", out _))
            {
                return null;
            }

            // Une la carpeta con la clave y comprueba que el archivo exista
            string archivo = Path.Combine(carpeta, clave);
            return File.Exists(archivo) ? archivo : null;
        }

        // Guarda el archivo que el controlador recibio del formulario
        // Devuelve su clave para registrar despues la referencia en PostgreSQL
        // El token permite cancelar la operacion cuando sea necesario
        public async Task<string> Guardar(IFormFile archivo, CancellationToken ct)
        {
            // Abre el contenido recibido para leerlo sin cargar todo el PDF en memoria
            await using Stream entrada = archivo.OpenReadStream();
            // Lee los primeros cinco bytes para comprobar la marca inicial de un PDF
            byte[] encabezado = new byte[5];
            await entrada.ReadExactlyAsync(encabezado, ct);
            if (!encabezado.AsSpan().SequenceEqual("%PDF-"u8))
            {
                throw new InvalidDataException("El archivo debe ser un PDF.");
            }

            // Crea la carpeta si todavia no existe
            Directory.CreateDirectory(carpeta);
            // Genera un nombre unico en lugar de usar el nombre enviado por el navegador
            string clave = Guid.NewGuid().ToString("N") + ".pdf";
            string ruta = Path.Combine(carpeta, clave);
            try
            {
                // Crea el archivo fisico en la carpeta configurada sin sobrescribir otro
                await using (FileStream salida = new FileStream(ruta, FileMode.CreateNew, FileAccess.Write))
                {
                    // Escribe el encabezado ya leido y copia el resto del contenido recibido
                    await salida.WriteAsync(encabezado, ct);
                    await entrada.CopyToAsync(salida, ct);
                }

                // Comprueba que el PDF guardado se pueda abrir antes de aceptar su clave
                ValidarDocumento(ruta);
                return clave;
            }
            catch
            {
                // Si falla el guardado o la validacion elimina el archivo y comunica el error
                File.Delete(ruta);
                throw;
            }
        }

        // Utiliza PdfPig para abrir el documento y leer su primera pagina
        private static void ValidarDocumento(string ruta)
        {
            try
            {
                using PdfDocument documento = PdfDocument.Open(ruta, new ParsingOptions { UseLenientParsing = false });
                // Rechaza documentos que no tengan paginas
                if (documento.NumberOfPages == 0)
                {
                    throw new InvalidDataException("El PDF no tiene páginas.");
                }

                documento.GetPage(1);
            }
            // Presenta los errores de lectura del PDF como un archivo no valido
            catch (Exception error)when (error is not (IOException or UnauthorizedAccessException or OutOfMemoryException))
            {
                throw new InvalidDataException("El archivo no contiene un PDF válido que se pueda abrir.", error);
            }
        }

        // Retira el PDF cuando la operacion que lo necesitaba no se completo
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
