using System.Text.Json.Serialization;

namespace APIScriptoriumArchivum.Dtos
{
    // Recibe los datos academicos necesarios para registrar o editar una ficha
    public class MetadatosFicha
    {
        public string? Titulo { get; init; }
        public int Anio { get; init; }
        public string? Resumen { get; init; }
        public int? IdCarrera { get; init; }
        public int? IdTipoTrabajo { get; init; }
        // Identificadores de los temas relacionados con el trabajo
        public int[]? AreaIds { get; init; }
        // Identificadores de las personas que participan como autores
        public int[]? AutorIds { get; init; }
        // Los campos de texto tambien admiten los nombres previstos por el contrato existente
        public string? Carrera { get; init; }
        public string? Tipo { get; init; }
        public string? Area { get; init; }
        public string[]? Autores { get; init; }

        // Confirma la posible ficha duplicada y conserva el nombre JSON de la interfaz
        [JsonPropertyName("confirmarDuplicado")]
        public bool ConfirmarDuplicado { get; init; }
    }

    // Contiene nombres y el identificador necesarios para registrar una persona
    public class PersonaDatos
    {
        public string? Nombres { get; init; }
        public string? Apellidos { get; init; }
        public string? IdentificadorInstitucional { get; init; }
    }

    // Recibe la referencia UES que se asociara al ejemplar del trabajo
    public class ReferenciaEjemplar
    {
        public string? Referencia { get; init; }
    }

    // Recoge la justificacion para retirar un trabajo de la consulta publica
    public class MotivoRetiro
    {
        public string? Motivo { get; init; }
    }

    // Reune verificaciones, permisos y decision de Biblioteca para un ejemplar concreto
    public class ControlesFicha
    {
        // Version exacta a la que se aplican estos controles
        public string? EjemplarId { get; init; }
        public string? DepositoReferencia { get; init; }
        public string? DepositoFecha { get; init; }
        public bool? DepositoVerificado { get; init; }
        public bool Verificar { get; init; }
        public string? ConformidadReferencia { get; init; }
        // Permisos independientes para mostrar la ficha, leer y descargar el PDF
        public bool? FichaPublica { get; init; }
        public bool? Lectura { get; init; }
        public bool? Descarga { get; init; }
        public ConstanciaDatos? Autorizacion { get; init; }
        public DecisionDatos? Decision { get; init; }
        // Permite registrar otra decision aunque coincida con la anterior
        public bool RegistrarDecision { get; init; }
    }

    // Describe la autorizacion de difusion y los permisos que conceden sus otorgantes
    public class ConstanciaDatos
    {
        public string? Referencia { get; init; }
        public string? Otorgantes { get; init; }
        // Identificadores de las personas que conceden la autorizacion
        public int[]? OtorganteIds { get; init; }
        public string? Fecha { get; init; }
        public string? Vence { get; init; }
        public string? Estado { get; init; }
        // Permisos concedidos por la constancia, distintos de los habilitados por Biblioteca
        public bool Ficha { get; init; }
        public bool Lectura { get; init; }
        public bool Descarga { get; init; }
    }

    // Transporta el resultado y motivo de la decision de difusion de Biblioteca
    public class DecisionDatos
    {
        public string? Resultado { get; init; }
        public string? Motivo { get; init; }
    }
}
