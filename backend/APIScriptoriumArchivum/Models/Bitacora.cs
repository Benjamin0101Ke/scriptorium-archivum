using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Conserva quien realizo una accion, sobre que registro y en que momento
public partial class Bitacora
{
    public int IdEvento { get; set; }
    public int? IdUsuarioActor { get; set; }
    public string? ActorTecnico { get; set; }
    public string Operacion { get; set; } = null!;
    public string TablaAfectada { get; set; } = null!;
    public string RegistroAfectado { get; set; } = null!;
    public DateTime FechaHora { get; set; }
    public string? Detalle { get; set; }
    public virtual ICollection<CorreosEventos> CorreosEventos { get; set; } = new List<CorreosEventos>();
    public virtual Usuarios? IdUsuarioActorNavigation { get; set; }
}
