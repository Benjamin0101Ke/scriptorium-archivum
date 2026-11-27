using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Conserva el historial de la tabla revisiones_docentes
// El flujo actual registra las nuevas revisiones docentes en Revisiones
public partial class RevisionesDocentes
{
    public long IdRevision { get; set; }
    public int IdTrabajo { get; set; }
    public int IdEntrega { get; set; }
    public int IdDocente { get; set; }
    public string Resultado { get; set; } = null!;
    public string Observaciones { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public Entregas Entregas { get; set; } = null!;
    public Usuarios IdDocenteNavigation { get; set; } = null!;
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
}
