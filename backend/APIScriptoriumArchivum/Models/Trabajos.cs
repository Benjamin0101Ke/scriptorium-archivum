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
    // Un trabajo puede tener cero o muchos archivos complementarios
    public ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    public Usuarios? DepositoVerificadoPorNavigation { get; set; }
    // Un trabajo puede recibir cero o muchas distinciones registradas
    public ICollection<DistincionesDestacado> DistincionesDestacado { get; set; } = new List<DistincionesDestacado>();
    // Un trabajo puede tener cero o muchas versiones de su ejemplar
    // Cada entrega de esta coleccion pertenece a ese trabajo
    public ICollection<Entregas> Entregas { get; set; } = new List<Entregas>();
    // Ejemplar final asociado a la ficha, no la lista completa de sus versiones
    public Entregas? EntregasNavigation { get; set; }
    // Un trabajo puede tener cero o muchas evidencias academicas registradas
    public ICollection<EvidenciasAcademicas> EvidenciasAcademicas { get; set; } = new List<EvidenciasAcademicas>();
    // Un trabajo puede aparecer en cero o muchos registros de favoritos
    // Favoritos relaciona muchos usuarios con muchos trabajos
    public ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();
    // Un trabajo puede tener cero o muchos registros de cambios de estado
    public ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public Carreras IdCarreraNavigation { get; set; } = null!;
    public Solicitudes? IdSolicitudNavigation { get; set; }
    public TiposTrabajo IdTipoTrabajoNavigation { get; set; } = null!;
    public Usuarios? IdUsuarioEnvioNavigation { get; set; }
    public Usuarios? IdUsuarioRetiraNavigation { get; set; }
    // Un trabajo puede estar asociado con cero o muchas notificaciones
    public ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    // Un trabajo puede tener cero o muchas palabras clave asociadas
    public ICollection<PalabrasClaveTrabajo> PalabrasClaveTrabajo { get; set; } = new List<PalabrasClaveTrabajo>();
    // Un trabajo puede tener cero o muchas participaciones de personas
    // Participaciones relaciona muchos trabajos con muchas personas
    public ICollection<Participaciones> Participaciones { get; set; } = new List<Participaciones>();
    // Un trabajo puede tener cero o muchos permisos asignados a usuarios
    // PermisosTrabajo relaciona muchos usuarios con muchos trabajos
    public ICollection<PermisosTrabajo> PermisosTrabajo { get; set; } = new List<PermisosTrabajo>();
    // Un trabajo puede tener cero o muchos recursos digitales asociados
    public ICollection<RecursosDigitales> RecursosDigitales { get; set; } = new List<RecursosDigitales>();
    // Un trabajo puede tener cero o muchos registros del historial docente separado
    public ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    // Un trabajo puede tener cero o muchas asociaciones con areas tematicas
    // TrabajosAreas une trabajos y areas en una relacion de muchos a muchos
    public ICollection<TrabajosAreas> TrabajosAreas { get; set; } = new List<TrabajosAreas>();
}
