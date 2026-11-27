using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Relaciona un trabajo con cada una de sus areas tematicas
// La tabla intermedia permite varios temas por trabajo y varios trabajos por tema
public partial class TrabajosAreas
{
    public int IdTrabajoArea { get; set; }
    public int IdTrabajo { get; set; }
    public int IdArea { get; set; }
    public AreasTematicas IdAreaNavigation { get; set; } = null!;
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
}
