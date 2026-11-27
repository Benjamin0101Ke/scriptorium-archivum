using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Describe un cambio de estado de una solicitud, entrega o trabajo
// Conserva el estado anterior, el nuevo y el motivo del cambio
public partial class HistorialEstados
{
    public int IdHistorial { get; set; }
    public int? IdSolicitud { get; set; }
    public int? IdEntrega { get; set; }
    public int? IdTrabajo { get; set; }
    public int IdUsuarioActor { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = null!;
    public DateTime FechaHora { get; set; }
    public string? Decision { get; set; }
    public string? Motivo { get; set; }
    public string Ambito { get; set; } = null!;
    public Entregas? IdEntregaNavigation { get; set; }
    public Solicitudes? IdSolicitudNavigation { get; set; }
    public Trabajos? IdTrabajoNavigation { get; set; }
    public Usuarios IdUsuarioActorNavigation { get; set; } = null!;
}
