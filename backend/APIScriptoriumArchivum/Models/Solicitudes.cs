using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Modela los datos de una solicitud del tramite academico previsto
// La tabla se conserva aunque ese tramite no este desarrollado por completo
public partial class Solicitudes
{
    public int IdSolicitud { get; set; }
    public int IdUsuarioSolicitante { get; set; }
    public int IdCarrera { get; set; }
    public int IdTipoTrabajo { get; set; }
    public string TituloProvisional { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public DateTime FechaPresentacion { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime? PlazoCorreccion { get; set; }
    // Una solicitud puede tener cero o muchos registros de cambios de estado
    public ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public Carreras IdCarreraNavigation { get; set; } = null!;
    public TiposTrabajo IdTipoTrabajoNavigation { get; set; } = null!;
    public Usuarios IdUsuarioSolicitanteNavigation { get; set; } = null!;
    // Una solicitud puede estar asociada con cero o muchas notificaciones
    public ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    public Trabajos? Trabajos { get; set; }
}
