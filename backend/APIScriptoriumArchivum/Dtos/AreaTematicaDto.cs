namespace APIScriptoriumArchivum.Dtos
{
    // Devuelve el identificador y nombre de un tema para los catalogos de la interfaz
    public class AreaTematicaDto
    {
        // Prepara los dos valores que se devuelven en el catalogo
        public AreaTematicaDto(int id, string nombre)
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
