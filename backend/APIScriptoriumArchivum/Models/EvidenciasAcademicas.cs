using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Almacena la referencia y el resultado de un respaldo academico del trabajo
public partial class EvidenciasAcademicas
{
    public int IdEvidencia { get; set; }
    public int IdTrabajo { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public string Resultado { get; set; } = null!;
    public DateOnly FechaEmision { get; set; }
    public string Emisor { get; set; } = null!;
    public string ReferenciaRespaldo { get; set; } = null!;
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
    public Usuarios IdUsuarioRegistraNavigation { get; set; } = null!;
}
