using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Contiene los nombres y el identificador de una persona, tenga o no una cuenta
public partial class Personas
{
    public int IdPersona { get; set; }
    public string Nombres { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string? IdentificadorInstitucional { get; set; }
    public virtual ICollection<Participaciones> Participaciones { get; set; } = new List<Participaciones>();
    public virtual ICollection<PersonasAutorizaciones> PersonasAutorizaciones { get; set; } = new List<PersonasAutorizaciones>();
    public virtual Usuarios? Usuarios { get; set; }
}
