using System.ComponentModel.DataAnnotations.Schema;

namespace APIScriptoriumArchivum.Datos
{
    // Reune el resultado de la consulta SQL usada para preparar el catalogo publico
    // Las columnas combinan datos del trabajo, carrera, autores y areas
    public class CandidatoFicha
    {
        // Column enlaza cada propiedad con el nombre que devuelve la consulta SQL
        [Column("id_trabajo")]
        public int IdTrabajo { get; set; }

        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("anio")]
        public short Anio { get; set; }

        [Column("resumen")]
        public string Resumen { get; set; } = string.Empty;

        [Column("carrera")]
        public string Carrera { get; set; } = string.Empty;

        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("autores")]
        public string[] Autores { get; set; } = [];

        [Column("areas")]
        public string[] Areas { get; set; } = [];
    }
}
