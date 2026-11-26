using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Representa los datos que devuelve la vista vw_estrellas_por_trabajo de PostgreSQL
// Cuenta los favoritos activos de cada trabajo, sin calcular calificaciones ni promedios
public partial class VwEstrellasPorTrabajo
{
    // Trabajo al que corresponde el conteo de la vista
    public int? IdTrabajo { get; set; }
    // Cantidad de favoritos activos, no una puntuacion del uno al cinco
    public long? Estrellas { get; set; }
}
