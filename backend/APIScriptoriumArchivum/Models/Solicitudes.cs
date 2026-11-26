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
    public virtual ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public virtual Carreras IdCarreraNavigation { get; set; } = null!;
    public virtual TiposTrabajo IdTipoTrabajoNavigation { get; set; } = null!;
    public virtual Usuarios IdUsuarioSolicitanteNavigation { get; set; } = null!;
    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    public virtual Trabajos? Trabajos { get; set; }
}
