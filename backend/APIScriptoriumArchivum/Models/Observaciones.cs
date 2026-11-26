using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Detalla una observacion de una revision y la respuesta que puede recibir
// Su existencia en el modelo no implica que todo el tramite academico este implementado
public partial class Observaciones
{
    public int IdObservacion { get; set; }
    public int IdRevision { get; set; }
    public string Descripcion { get; set; } = null!;
    public string EstadoAtencion { get; set; } = null!;
    public string? Respuesta { get; set; }
    public int? IdUsuarioResponde { get; set; }
    public DateTime FechaObservacion { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public virtual Revisiones IdRevisionNavigation { get; set; } = null!;
    public virtual Usuarios? IdUsuarioRespondeNavigation { get; set; }
}
