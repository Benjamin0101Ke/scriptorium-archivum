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

    // Un usuario puede adjuntar cero o muchos archivos complementarios
    public ICollection<ArchivosComplementarios> ArchivosComplementarios { get; set; } = new List<ArchivosComplementarios>();
    // Un usuario puede registrar cero o muchas autorizaciones de difusion
    // Esta relacion identifica quien las registra y no a sus otorgantes
    public ICollection<AutorizacionesDifusion> AutorizacionesDifusion { get; set; } = new List<AutorizacionesDifusion>();
    // Un usuario puede ser el actor de cero o muchos eventos de bitacora
    public ICollection<Bitacora> Bitacora { get; set; } = new List<Bitacora>();
    // Un usuario puede estar asociado con cero o muchos intentos de correo de recuperacion
    public ICollection<CorreosIntentos> CorreosIntentos { get; set; } = new List<CorreosIntentos>();
    // Un usuario puede seleccionar cero o muchas distinciones de trabajos
    public ICollection<DistincionesDestacado> DistincionesDestacado { get; set; } = new List<DistincionesDestacado>();
    // Un usuario puede enviar cero o muchas entregas de trabajos
    public ICollection<Entregas> EntregasIdUsuarioEnviaNavigation { get; set; } = new List<Entregas>();
    // Un usuario puede verificar cero o muchas entregas de trabajos
    public ICollection<Entregas> EntregasVerificadoPorNavigation { get; set; } = new List<Entregas>();
    // Un usuario puede registrar cero o muchas evidencias academicas
    public ICollection<EvidenciasAcademicas> EvidenciasAcademicas { get; set; } = new List<EvidenciasAcademicas>();
    // Un usuario puede mantener cero o muchos registros de favoritos
    // Favoritos relaciona muchos usuarios con muchos trabajos
    public ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();
    // Un usuario puede ser el actor de cero o muchos cambios de estado
    public ICollection<HistorialEstados> HistorialEstados { get; set; } = new List<HistorialEstados>();
    public Carreras? IdCarreraNavigation { get; set; }
    // Cuenta del docente asignado a este estudiante
    public Usuarios? IdDocenteNavigation { get; set; }
    public Personas IdPersonaNavigation { get; set; } = null!;
    public PlanesEstudio? IdPlanNavigation { get; set; }
    public Roles IdRolNavigation { get; set; } = null!;
    // Una cuenta docente puede tener cero o muchos estudiantes asignados
    // Es una relacion de Usuarios consigo misma mediante IdDocente
    public ICollection<Usuarios> InverseIdDocenteNavigation { get; set; } = new List<Usuarios>();
    // Un usuario puede recibir cero o muchas notificaciones
    public ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();
    // Un usuario puede responder cero o muchas observaciones
    public ICollection<Observaciones> Observaciones { get; set; } = new List<Observaciones>();
    // Un usuario puede tener cero o muchos permisos sobre trabajos
    // PermisosTrabajo relaciona muchos usuarios con muchos trabajos
    public ICollection<PermisosTrabajo> PermisosTrabajo { get; set; } = new List<PermisosTrabajo>();
    // Un usuario puede tener cero o muchos registros de presencia de sesiones
    public ICollection<PresenciaSesiones> PresenciaSesiones { get; set; } = new List<PresenciaSesiones>();
    public RecuperacionesContrasena? RecuperacionesContrasena { get; set; }
    // Un usuario revisor puede realizar cero o muchas revisiones de entregas
    public ICollection<Revisiones> Revisiones { get; set; } = new List<Revisiones>();
    // Una cuenta docente puede tener cero o muchos registros del historial docente separado
    public ICollection<RevisionesDocentes> RevisionesDocentes { get; set; } = new List<RevisionesDocentes>();
    // Un usuario puede presentar cero o muchas solicitudes
    public ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
    // Un usuario puede verificar el deposito de cero o muchos trabajos
    public ICollection<Trabajos> TrabajosDepositoVerificadoPorNavigation { get; set; } = new List<Trabajos>();
    // Un usuario puede ser el remitente de cero o muchos trabajos
    public ICollection<Trabajos> TrabajosIdUsuarioEnvioNavigation { get; set; } = new List<Trabajos>();
    // Un usuario puede retirar de la consulta publica cero o muchos trabajos
    public ICollection<Trabajos> TrabajosIdUsuarioRetiraNavigation { get; set; } = new List<Trabajos>();
}
