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
    // Una entrega puede tener cero o muchos archivos complementarios asociados
    public ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    // Una entrega puede conservar cero o muchas autorizaciones en su historial
    // La restriccion de la base permite solo una autorizacion activa por entrega
    public ICollection<AutorizacionesDifusion> AutorizacionesDifusion { get; set; } = new List<AutorizacionesDifusion>();
    // Una entrega puede tener cero o muchos registros de cambios de estado
    public ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
    public Usuarios IdUsuarioEnviaNavigation { get; set; } = null!;
    // Una entrega puede estar asociada con cero o muchas notificaciones
    public ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    // Una entrega puede tener cero o muchos recursos digitales asociados
    // Los recursos deben pertenecer al mismo trabajo que la entrega
    public ICollection<RecursosDigitales> RecursosDigitales { get; set; } = new List<RecursosDigitales>();
    // Una entrega puede recibir cero o muchas revisiones en su historial
    public ICollection<Revisiones> Revisiones { get; set; } = new List<Revisiones>();
    // Una entrega puede tener cero o muchos registros del historial docente separado
    public ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    // Coleccion del trabajo que selecciona esta entrega como ejemplar final
    // Las claves de la base limitan esta relacion a cero o un trabajo
    public ICollection<Trabajos> Trabajos { get; set; } = new List<Trabajos>();
    public Usuarios? VerificadoPorNavigation { get; set; }
}
