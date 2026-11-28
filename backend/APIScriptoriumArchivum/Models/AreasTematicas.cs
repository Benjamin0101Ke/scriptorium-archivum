using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Define los temas que sirven para clasificar los trabajos del catalogo
public partial class AreasTematicas
{
    public int IdArea { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; }
    // Un area puede tener cero o muchas asociaciones con trabajos
    // TrabajosAreas une trabajos y areas en una relacion de muchos a muchos
    public ICollection<TrabajosAreas> TrabajosAreas { get; set; } = new List<TrabajosAreas>();
}
