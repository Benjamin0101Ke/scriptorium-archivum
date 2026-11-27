using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Modela un aviso de correo y su estado dentro de la cola de envio
// Incluye la proxima fecha de intento y la reserva del trabajador que lo procesa
public partial class CorreosEventos
{
    public long IdCorreo { get; set; }
    public int IdEvento { get; set; }
    public string Destino { get; set; } = null!;
    public string Asunto { get; set; } = null!;
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public int Intentos { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime ProximoIntento { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime? EnviadoEn { get; set; }
    public string? ErrorTipo { get; set; }
    // Distingue los intentos iniciales de un nuevo ciclo solicitado por reenvio
    public int Ciclo { get; set; }
    // Identifica al trabajador que reservo temporalmente este aviso
    public Guid? Reserva { get; set; }
    // Un aviso de correo puede tener cero o muchos intentos de envio
    public ICollection<CorreosIntentos> CorreosIntentos { get; set; } = new List<CorreosIntentos>();
    public Bitacora IdEventoNavigation { get; set; } = null!;
}
