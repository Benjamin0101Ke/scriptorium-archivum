using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Conserva el resultado de cada intento de envio de correo
// Distingue los avisos de actividad de los correos de recuperacion
public partial class CorreosIntentos
{
    public long IdIntento { get; set; }
    // Aviso relacionado cuando el intento pertenece a la cola de eventos
    public long? IdCorreo { get; set; }
    // Cuenta relacionada cuando se intenta enviar un codigo de recuperacion
    public int? IdUsuario { get; set; }
    public string Categoria { get; set; } = null!;
    public int Ciclo { get; set; }
    public int NumeroIntento { get; set; }
    public string Resultado { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public string? ErrorTipo { get; set; }
    public CorreosEventos? IdCorreoNavigation { get; set; }
    public Usuarios? IdUsuarioNavigation { get; set; }
}
