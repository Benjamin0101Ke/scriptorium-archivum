using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Relaciona un trabajo destacado con su criterio, responsable y periodo de vigencia
public partial class DistincionesDestacado
{
    public int IdDistincion { get; set; }
    public int IdTrabajo { get; set; }
    public int IdUsuarioSelecciona { get; set; }
    public string Criterio { get; set; } = null!;
    public string Motivo { get; set; } = null!;
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public virtual Trabajos IdTrabajoNavigation { get; set; } = null!;
    public virtual Usuarios IdUsuarioSeleccionaNavigation { get; set; } = null!;
}
