using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Guarda los datos de los anexos de un trabajo, como un ZIP u otro documento
// El archivo queda en la carpeta de almacenamiento y la tabla conserva su referencia
public partial class ArchivosComplementarios
{
    public long IdArchivo { get; set; }
    public int IdTrabajo { get; set; }
    public int IdUsuario { get; set; }
    // Entrega a la que se adjunto el archivo, si tiene una asociada
    public int? IdEntrega { get; set; }
    public string Nombre { get; set; } = null!;
    // Nombre interno utilizado para localizar el anexo en almacenamiento
    public string Clave { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public long TamanoBytes { get; set; }
    // Indica si Biblioteca habilito este anexo para su difusion
    public bool Habilitado { get; set; }
    public DateTime FechaRegistro { get; set; }
    public Entregas? IdEntregaNavigation { get; set; }
    public Trabajos IdTrabajoNavigation { get; set; } = null!;
    public Usuarios IdUsuarioNavigation { get; set; } = null!;
}
