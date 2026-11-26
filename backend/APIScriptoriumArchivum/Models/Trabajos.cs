using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Representa la ficha del trabajo con sus datos academicos y estado de publicacion
// Las versiones del ejemplar se guardan por separado en Entregas
public partial class Trabajos
{
    public int IdTrabajo { get; set; }
    public string Codigo { get; set; } = null!;
    public int IdTipoTrabajo { get; set; }
    public int IdCarrera { get; set; }
    public int? IdSolicitud { get; set; }
    // Identifica el ejemplar final seleccionado para este mismo trabajo
    public int? IdEntregaFinal { get; set; }
    public string Titulo { get; set; } = null!;
    public short Anio { get; set; }
    public string Resumen { get; set; } = null!;
    public string? Objetivos { get; set; }
    public string? Resultados { get; set; }
    public string EstadoPublicacion { get; set; } = null!;
    public string? NivelAcceso { get; set; }
    public DateTime? FechaHabilitacion { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public DateTime? FechaRetiro { get; set; }
    public string? MotivoRetiro { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string? ReferenciaDepositoUes { get; set; }
    public int? DepositoVerificadoPor { get; set; }
    public DateTime? DepositoVerificadoEn { get; set; }
    public int? IdUsuarioRetira { get; set; }
    public int? IdUsuarioEnvio { get; set; }
    public virtual ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    public virtual Usuarios? DepositoVerificadoPorNavigation { get; set; }
    public virtual ICollection<DistincionesDestacado> DistincionesDestacado { get; set; } = new List<DistincionesDestacado>();
    // Versiones del ejemplar que pertenecen a este trabajo
    public virtual ICollection<Entregas> Entregas { get; set; } = new List<Entregas>();
    // Ejemplar final asociado a la ficha, no la lista completa de sus versiones
    public virtual Entregas? EntregasNavigation { get; set; }
    public virtual ICollection<EvidenciasAcademicas> EvidenciasAcademicas { get; set; } = new List<EvidenciasAcademicas>();
    public virtual ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();
    public virtual ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public virtual Carreras IdCarreraNavigation { get; set; } = null!;
    public virtual Solicitudes? IdSolicitudNavigation { get; set; }
    public virtual TiposTrabajo IdTipoTrabajoNavigation { get; set; } = null!;
    public virtual Usuarios? IdUsuarioEnvioNavigation { get; set; }
    public virtual Usuarios? IdUsuarioRetiraNavigation { get; set; }
    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    public virtual ICollection<PalabrasClaveTrabajo> PalabrasClaveTrabajo { get; set; } = new List<PalabrasClaveTrabajo>();
    public virtual ICollection<Participaciones> Participaciones { get; set; } = new List<Participaciones>();
    public virtual ICollection<PermisosTrabajo> PermisosTrabajo { get; set; } = new List<PermisosTrabajo>();
    public virtual ICollection<RecursosDigitales> RecursosDigitales { get; set; } = new List<RecursosDigitales>();
    public virtual ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    public virtual ICollection<TrabajosAreas> TrabajosAreas { get; set; } = new List<TrabajosAreas>();
}
