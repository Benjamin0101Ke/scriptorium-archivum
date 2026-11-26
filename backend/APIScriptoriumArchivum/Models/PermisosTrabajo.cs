using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Registra un permiso especifico de un usuario sobre un trabajo
// Este permiso no sustituye las verificaciones ni la constancia de difusion del ejemplar
public partial class PermisosTrabajo
{
    public int IdPermiso { get; set; }
    public int IdUsuario { get; set; }
    public int IdTrabajo { get; set; }
    public string NivelPermiso { get; set; } = null!;
    public bool Activo { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public virtual Trabajos IdTrabajoNavigation { get; set; } = null!;
    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
