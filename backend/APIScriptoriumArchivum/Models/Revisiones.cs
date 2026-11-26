using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Registra la revision de un ejemplar, su responsable y la decision tomada
// El ambito distingue la revision docente de la decision de difusion de Biblioteca
public partial class Revisiones
{
    public int IdRevision { get; set; }
    public int IdEntrega { get; set; }
    public int IdUsuarioRevisor { get; set; }
    public int? IdProcedimiento { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    // Determina que clase de revision se realizo sobre el ejemplar
    public string AmbitoRevision { get; set; } = null!;
    public string? Resultado { get; set; }
    public string? MotivoDecision { get; set; }
    public virtual Entregas IdEntregaNavigation { get; set; } = null!;
    public virtual ProcedimientosDeposito? IdProcedimientoNavigation { get; set; }
    public virtual Usuarios IdUsuarioRevisorNavigation { get; set; } = null!;
    public virtual ICollection<Observaciones> Observaciones { get; set; } = new List<Observaciones>();
}
