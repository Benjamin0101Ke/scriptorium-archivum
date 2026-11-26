namespace APIScriptoriumArchivum.Datos
{
    public class ErrorApi : Exception
    {
        public ErrorApi(int estado, string mensaje, object? respuesta = null) : base(mensaje)
        {
            Estado = estado;
            Respuesta = respuesta ?? new
            {
                error = mensaje
            };
        }

        public int Estado { get; }
        public object Respuesta { get; }
    }
}
