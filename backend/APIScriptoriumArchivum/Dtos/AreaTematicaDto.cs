namespace APIScriptoriumArchivum.Dtos
{
    public class AreaTematicaDto
    {
        public AreaTematicaDto(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public int Id { get; }
        public string Nombre { get; }
    }
}
