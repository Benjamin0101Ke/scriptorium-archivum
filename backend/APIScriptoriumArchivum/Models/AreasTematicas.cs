using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Define los temas que sirven para clasificar los trabajos del catalogo
public partial class AreasTematicas
{
    public int IdArea { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activa { get; set; }
    public virtual ICollection<TrabajosAreas> TrabajosAreas { get; set; } = new List<TrabajosAreas>();
}
