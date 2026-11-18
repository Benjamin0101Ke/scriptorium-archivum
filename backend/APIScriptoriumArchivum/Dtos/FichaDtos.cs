using System.Text.Json.Serialization;

namespace APIScriptoriumArchivum.Dtos
{
    public class MetadatosFicha
    {
        public string? Titulo { get; init; }
        public int Anio { get; init; }
        public string? Resumen { get; init; }
        public int? IdCarrera { get; init; }
        public int? IdTipoTrabajo { get; init; }
        public int[]? AreaIds { get; init; }
        public int[]? AutorIds { get; init; }
        public string? Carrera { get; init; }
        public string? Tipo { get; init; }
        public string? Area { get; init; }
        public string[]? Autores { get; init; }
        [JsonPropertyName("confirmarDuplicado")]
        public bool ConfirmarDuplicado { get; init; }
    }

    public class PersonaDatos
    {
        public string? Nombres { get; init; }
        public string? Apellidos { get; init; }
        public string? IdentificadorInstitucional { get; init; }
    }

    public class ReferenciaEjemplar
    {
        public string? Referencia { get; init; }
    }

    public class MotivoRetiro
    {
        public string? Motivo { get; init; }
    }

    public class ControlesFicha
    {
        public string? EjemplarId { get; init; }
        public string? DepositoReferencia { get; init; }
        public string? DepositoFecha { get; init; }
        public bool? DepositoVerificado { get; init; }
        public bool Verificar { get; init; }
        public string? ConformidadReferencia { get; init; }
        public bool? FichaPublica { get; init; }
        public bool? Lectura { get; init; }
        public bool? Descarga { get; init; }
        public ConstanciaDatos? Autorizacion { get; init; }
        public DecisionDatos? Decision { get; init; }
        public bool RegistrarDecision { get; init; }
    }

    public class ConstanciaDatos
    {
        public string? Referencia { get; init; }
        public string? Otorgantes { get; init; }
        public int[]? OtorganteIds { get; init; }
        public string? Fecha { get; init; }
        public string? Vence { get; init; }
        public string? Estado { get; init; }
        public bool Ficha { get; init; }
        public bool Lectura { get; init; }
        public bool Descarga { get; init; }
    }

    public class DecisionDatos
    {
        public string? Resultado { get; init; }
        public string? Motivo { get; init; }
    }
}
