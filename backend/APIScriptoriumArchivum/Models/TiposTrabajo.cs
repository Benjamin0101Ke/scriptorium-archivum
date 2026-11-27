using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Clasifica los trabajos e indica si su tipo requiere aprobacion academica
public partial class TiposTrabajo
{
    public int IdTipoTrabajo { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool RequiereAprobacionAcademica { get; set; }
    public bool Activa { get; set; }
    // Un tipo de trabajo puede clasificar cero o muchas solicitudes
    public ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    // Un tipo de trabajo puede clasificar cero o muchos trabajos
    public ICollection<Trabajos> Trabajos { get; set; } = new List<Trabajos>();
}
