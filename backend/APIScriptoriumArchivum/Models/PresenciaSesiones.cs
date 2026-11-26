using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Registra la actividad reciente de una sesion para consultar usuarios conectados
// No almacena la cookie ni sustituye la autenticacion
public partial class PresenciaSesiones
{
    public Guid IdSesion { get; set; }
    public int IdUsuario { get; set; }
    public string VersionSesion { get; set; } = null!;
    public DateTime UltimaActividad { get; set; }
    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
