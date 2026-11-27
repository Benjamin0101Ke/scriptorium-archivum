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
    public Entregas? IdEntregaNavigation { get; set; }
    public Solicitudes? IdSolicitudNavigation { get; set; }
    public Trabajos? IdTrabajoNavigation { get; set; }
    public Usuarios IdUsuarioNavigation { get; set; } = null!;
}
