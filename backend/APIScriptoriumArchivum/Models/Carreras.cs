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
    public Departamentos? IdDepartamentoNavigation { get; set; }
    // Una carrera puede tener cero o muchos planes de estudio
    public ICollection<PlanesEstudio> PlanesEstudio { get; set; } = new List<PlanesEstudio>();
    // Una carrera puede estar asociada con cero o muchas solicitudes
    public ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    // Una carrera puede tener cero o muchos trabajos registrados
    public ICollection<Trabajos> Trabajos { get; set; } = new List<Trabajos>();
    // Una carrera puede estar asociada con cero o muchas cuentas de usuarios
    public ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
