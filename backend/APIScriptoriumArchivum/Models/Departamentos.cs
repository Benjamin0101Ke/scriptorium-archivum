using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Agrupa carreras por departamento academico, no por tema del trabajo
public partial class Departamentos
{
    public int IdDepartamento { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; }
    public virtual ICollection<Carreras> Carreras { get; set; } = new List<Carreras>();
}
