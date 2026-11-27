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
    // Una persona puede tener cero o muchas participaciones en trabajos
    // Participaciones relaciona muchas personas con muchos trabajos
    public ICollection<Participaciones> Participaciones { get; set; } = new List<Participaciones>();
    // Una persona puede figurar en cero o muchos registros de otorgamiento
    // PersonasAutorizaciones relaciona muchas personas con muchas autorizaciones
    public ICollection<PersonasAutorizaciones> PersonasAutorizaciones { get; set; } = new List<PersonasAutorizaciones>();
    public Usuarios? Usuarios { get; set; }
}
