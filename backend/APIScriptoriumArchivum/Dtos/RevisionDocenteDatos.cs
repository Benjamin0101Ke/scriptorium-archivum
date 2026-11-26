namespace APIScriptoriumArchivum.Dtos
{
    // Recibe la revision del docente y el identificador del ejemplar revisado
    public class RevisionDocenteDatos
    {
        public string? Resultado { get; init; }
        public string? Observaciones { get; init; }
        // Admite el identificador como texto, segun el contrato de la interfaz
        public string? EntregaId { get; init; }
        // Alternativa numerica para identificar la misma entrega
        public int? EjemplarId { get; init; }
    }
}
