using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Representa un aviso interno dirigido a un usuario y su fecha de lectura
public partial class Notificaciones
{
    public int IdNotificacion { get; set; }
    public int? IdSolicitud { get; set; }
    public int? IdEntrega { get; set; }
    public int? IdTrabajo { get; set; }
    public string TipoEvento { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public DateTime FechaCreacion { get; set; }
    public int IdUsuario { get; set; }
    public DateTime? FechaLectura { get; set; }
    public virtual Entregas? IdEntregaNavigation { get; set; }
    public virtual Solicitudes? IdSolicitudNavigation { get; set; }
    public virtual Trabajos? IdTrabajoNavigation { get; set; }
    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
