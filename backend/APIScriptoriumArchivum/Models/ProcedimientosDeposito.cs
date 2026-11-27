using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Describe la version y vigencia de un procedimiento de deposito academico
public partial class ProcedimientosDeposito
{
    public int IdProcedimiento { get; set; }
    public string VersionProcedimiento { get; set; } = null!;
    public string ReferenciaDocumento { get; set; } = null!;
    public DateOnly FechaAprobacion { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    // Un procedimiento de deposito puede ser referencia de cero o muchas revisiones
    public ICollection<Revisiones> Revisiones { get; set; } = new List<Revisiones>();
}
