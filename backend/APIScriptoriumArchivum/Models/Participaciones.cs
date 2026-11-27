using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Vincula personas con trabajos e indica su participacion y orden de autoria
// Una persona autora puede existir sin tener una cuenta de acceso
public partial class Participaciones
{
    public int IdParticipacion { get; set; }
    public int IdPersona { get; set; }
    public int IdTrabajo { get; set; }
    // Indica el tipo de participacion, por ejemplo Autor
    public string CalidadParticipacion { get; set; } = null!;
    public short? OrdenAutoria { get; set; }
    public Personas IdPersonaNavigation { get; set; } = null!;
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
}
