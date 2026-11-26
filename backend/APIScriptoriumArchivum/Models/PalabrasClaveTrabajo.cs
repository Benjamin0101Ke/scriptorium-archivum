using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Asocia terminos de busqueda con un trabajo especifico
public partial class PalabrasClaveTrabajo
{
    public int IdPalabraTrabajo { get; set; }
    public int IdTrabajo { get; set; }
    public string Termino { get; set; } = null!;
    public virtual Trabajos IdTrabajoNavigation { get; set; } = null!;
}
