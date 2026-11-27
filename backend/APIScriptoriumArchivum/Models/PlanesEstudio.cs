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
    public Carreras IdCarreraNavigation { get; set; } = null!;
    // Un plan de estudio puede estar asociado con cero o muchas cuentas de usuarios
    public ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
