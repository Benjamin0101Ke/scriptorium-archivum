using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Describe una version del pensum de una carrera y el enlace a su documento
// No representa las materias ni los ciclos que componen ese plan
public partial class PlanesEstudio
{
    public int IdPlan { get; set; }
    public int IdCarrera { get; set; }
    public string Nombre { get; set; } = null!;
    public string? DocumentoUrl { get; set; }
    public bool Activa { get; set; }
    public virtual Carreras IdCarreraNavigation { get; set; } = null!;
    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
