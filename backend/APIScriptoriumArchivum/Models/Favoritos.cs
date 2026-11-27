using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Une una cuenta con un trabajo marcado como favorito
// La marca activa suma una estrella y la inactiva conserva el registro sin contarlo
public partial class Favoritos
{
    public int IdFavorito { get; set; }
    public int IdUsuario { get; set; }
    public int IdTrabajo { get; set; }
    // False conserva la pareja usuario y trabajo, pero la excluye del conteo de estrellas
    public bool Activo { get; set; }
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
    public Usuarios IdUsuarioNavigation { get; set; } = null!;
}
