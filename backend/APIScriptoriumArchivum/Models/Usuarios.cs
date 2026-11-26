using System;
using System.Collections.Generic;

namespace APIScriptoriumArchivum.Models;
// Modela la cuenta de acceso de una persona, con su rol y datos de perfil
// Tambien permite asociar carrera, plan y docente cuando corresponde
public partial class Usuarios
{
    public int IdUsuario { get; set; }
    public int IdPersona { get; set; }
    public string CorreoInstitucional { get; set; } = null!;
    public string Estado { get; set; } = null!;
    public DateTime FechaAlta { get; set; }
    public string? ProveedorIdentidad { get; set; }
    // Contrasena transformada mediante hash, no su valor original
    public string HashContrasena { get; set; } = null!;
    public int IdRol { get; set; }
    public string? NombreMostrar { get; set; }
    public string Avatar { get; set; } = null!;
    public string Tema { get; set; } = null!;
    // Permite invalidar sesiones cuando cambian el rol u otros datos de acceso
    public int VersionRol { get; set; }
    // Contenido de la foto guardado en PostgreSQL como bytes
    public byte[]? FotoPerfil { get; set; }
    public string? FotoTipo { get; set; }
    public Guid? FotoVersion { get; set; }
    public int? IdCarrera { get; set; }
    public int? IdPlan { get; set; }
    public int? IdDocente { get; set; }
    // ICollection agrupa registros relacionados y no corresponde a una columna
    // La lista empieza vacia y los datos se cargan mediante consultas
    // virtual permite sobrescribir la propiedad, no activa por si solo la carga automatica
    // Anexos que esta cuenta adjunto a trabajos
    public virtual ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    public virtual ICollection<AutorizacionesDifusion> AutorizacionesDifusion { get; set; } = new List<AutorizacionesDifusion>();
    public virtual ICollection<Bitacora> Bitacora { get; set; } = new List<Bitacora>();
    public virtual ICollection<CorreosIntentos> CorreosIntentos { get; set; } = new List<CorreosIntentos>();
    public virtual ICollection<DistincionesDestacado> DistincionesDestacado { get; set; } = new List<DistincionesDestacado>();
    public virtual ICollection<Entregas> EntregasIdUsuarioEnviaNavigation { get; set; } = new List<Entregas>();
    public virtual ICollection<Entregas> EntregasVerificadoPorNavigation { get; set; } = new List<Entregas>();
    public virtual ICollection<EvidenciasAcademicas> EvidenciasAcademicas { get; set; } = new List<EvidenciasAcademicas>();
    public virtual ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();
    public virtual ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public virtual Carreras? IdCarreraNavigation { get; set; }
    // Cuenta del docente asignado a este estudiante
    public virtual Usuarios? IdDocenteNavigation { get; set; }
    public virtual Personas IdPersonaNavigation { get; set; } = null!;
    public virtual PlanesEstudio? IdPlanNavigation { get; set; }
    public virtual Roles IdRolNavigation { get; set; } = null!;
    // Estudiantes que tienen a esta cuenta asignada como docente
    public virtual ICollection<Usuarios> InverseIdDocenteNavigation { get; set; } = new List<Usuarios>();
    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    public virtual ICollection<Observaciones> Observaciones { get; set; } = new List<Observaciones>();
    public virtual ICollection<PermisosTrabajo> PermisosTrabajo { get; set; } = new List<PermisosTrabajo>();
    public virtual ICollection<PresenciaSesiones> PresenciaSesiones { get; set; } = new List<PresenciaSesiones>();
    public virtual RecuperacionesContrasena? RecuperacionesContrasena { get; set; }
    public virtual ICollection<Revisiones> Revisiones { get; set; } = new List<Revisiones>();
    public virtual ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    public virtual ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    public virtual ICollection<Trabajos> TrabajosDepositoVerificadoPorNavigation { get; set; } = new List<Trabajos>();
    public virtual ICollection<Trabajos> TrabajosIdUsuarioEnvioNavigation { get; set; } = new List<Trabajos>();
    public virtual ICollection<Trabajos> TrabajosIdUsuarioRetiraNavigation { get; set; } = new List<Trabajos>();
}
