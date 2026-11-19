namespace APIScriptoriumArchivum.Dtos
{
    public class TipoDeTrabajoDto
    {
        public TipoDeTrabajoDto(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public int Id { get; }
        public string Nombre { get; }
    }
}
