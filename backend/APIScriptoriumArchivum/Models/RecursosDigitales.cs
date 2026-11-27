using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Describe un PDF, enlace o referencia UES asociado a un trabajo
// Ubicacion contiene la referencia del recurso, no sus bytes
public partial class RecursosDigitales
{
    public int IdRecurso { get; set; }
    public int? IdEntrega { get; set; }
    public string Clase { get; set; } = null!;
    // Clave de archivo, enlace o referencia segun la clase del recurso
    public string Ubicacion { get; set; } = null!;
    public string? Nombre { get; set; }
    public string? TipoFormato { get; set; }
    public long? TamanoBytes { get; set; }
    public string? EstadoCarga { get; set; }
    public int IdTrabajo { get; set; }
    public string? Descripcion { get; set; }
    public bool DifusionHabilitada { get; set; }
    public Entregas? Entregas { get; set; }
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
}
