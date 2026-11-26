using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Representa una version del ejemplar asociado a un trabajo
// Cada version conserva sus verificaciones, constancias y revisiones
public partial class Entregas
{
    public int IdEntrega { get; set; }
    public int IdTrabajo { get; set; }
    public int NumeroVersion { get; set; }
    public int IdUsuarioEnvia { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateOnly? FechaDeposito { get; set; }
    public string Estado { get; set; } = null!;
    public string? NotaCambios { get; set; }
    public string? ReferenciaConformidad { get; set; }
    public int? VerificadoPor { get; set; }
    public DateTime? VerificadoEn { get; set; }
    // Permisos de este ejemplar, que ademas requieren una constancia valida
    public bool DifundirFichaHabilitado { get; set; }
    public bool LeerPdfHabilitado { get; set; }
    public bool DescargarPdfHabilitado { get; set; }
    public virtual ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    public virtual ICollection<AutorizacionesDifusion> AutorizacionesDifusion { get; set; } = new List<AutorizacionesDifusion>();
    public virtual ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public virtual Trabajos IdTrabajoNavigation { get; set; } = null!;
    public virtual Usuarios IdUsuarioEnviaNavigation { get; set; } = null!;
    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    public virtual ICollection<RecursosDigitales> RecursosDigitales { get; set; } = new List<RecursosDigitales>();
    public virtual ICollection<Revisiones> Revisiones { get; set; } = new List<Revisiones>();
    public virtual ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    public virtual ICollection<Trabajos> Trabajos { get; set; } = new List<Trabajos>();
    public virtual Usuarios? VerificadoPorNavigation { get; set; }
}
