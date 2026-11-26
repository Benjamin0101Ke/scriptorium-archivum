using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Describe las carreras academicas que se asocian a trabajos y estudiantes
public partial class Carreras
{
    public int IdCarrera { get; set; }
    public string CodigoCarrera { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool Activa { get; set; }
    public int? IdDepartamento { get; set; }
    public virtual Departamentos? IdDepartamentoNavigation { get; set; }
    public virtual ICollection<PlanesEstudio> PlanesEstudio { get; set; } = new List<PlanesEstudio>();
    public virtual ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    public virtual ICollection<Trabajos> Trabajos { get; set; } = new List<Trabajos>();
    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
