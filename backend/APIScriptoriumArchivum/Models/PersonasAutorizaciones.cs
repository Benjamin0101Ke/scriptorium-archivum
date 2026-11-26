using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Identifica a las personas que otorgan una constancia de difusion
// Una misma autorizacion puede tener varios otorgantes
public partial class PersonasAutorizaciones
{
    public int IdOtorgante { get; set; }
    public int IdPersona { get; set; }
    public int IdAutorizacion { get; set; }
    public virtual AutorizacionesDifusion IdAutorizacionNavigation { get; set; } = null!;
    public virtual Personas IdPersonaNavigation { get; set; } = null!;
}
