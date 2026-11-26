using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Define los roles que utiliza la API para comprobar los permisos de las cuentas
public partial class Roles
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
