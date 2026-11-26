using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Registra la constancia de difusion de un ejemplar y los permisos concedidos
// Una entrega nueva necesita su propia autorizacion
public partial class AutorizacionesDifusion
{
    public int IdAutorizacion { get; set; }
    public int IdEntrega { get; set; }
    public int IdUsuarioRegistra { get; set; }
    public DateTime FechaOtorgamiento { get; set; }
    public string Alcance { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public string ReferenciaRespaldo { get; set; } = null!;
    public DateOnly? VigenciaHasta { get; set; }
    // Permisos concedidos por la constancia, separados de los habilitados en la entrega
    public bool DifundirFichaConcedido { get; set; }
    public bool LeerPdfConcedido { get; set; }
    public bool DescargarPdfConcedido { get; set; }
    public virtual Entregas IdEntregaNavigation { get; set; } = null!;
    public virtual Usuarios IdUsuarioRegistraNavigation { get; set; } = null!;
    public virtual ICollection<PersonasAutorizaciones> PersonasAutorizaciones { get; set; } = new List<PersonasAutorizaciones>();
}
