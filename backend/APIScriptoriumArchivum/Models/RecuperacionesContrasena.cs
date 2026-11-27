using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Guarda el estado temporal de recuperacion de una cuenta y sus limites de intentos
// El codigo se conserva protegido y cada usuario tiene como maximo un registro
public partial class RecuperacionesContrasena
{
    public int IdUsuario { get; set; }
    // Codigo protegido con Data Protection, no guardado como texto legible
    public string? CodigoProtegido { get; set; }
    public DateTime VenceEn { get; set; }
    public DateTime UltimoEnvio { get; set; }
    public DateTime InicioVentana { get; set; }
    public int EnviosVentana { get; set; }
    public int Intentos { get; set; }
    public Usuarios IdUsuarioNavigation { get; set; } = null!;
}
