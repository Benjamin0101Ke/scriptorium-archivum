namespace APIScriptoriumArchivum.Dtos
{
    // Entrega los datos basicos de un tipo de trabajo para su seleccion
    public class TipoDeTrabajoDto
    {
        // Prepara los dos valores que se devuelven en el catalogo
        public TipoDeTrabajoDto(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        // Identificador del registro en su catalogo
        public int Id { get; }
        // Nombre que se muestra en los formularios
        public string Nombre { get; }
    }
}
