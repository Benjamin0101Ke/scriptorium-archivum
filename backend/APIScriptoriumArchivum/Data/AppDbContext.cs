using System;
using System.Collections.Generic;
using APIScriptoriumArchivum.Models;
using Microsoft.EntityFrameworkCore;

namespace APIScriptoriumArchivum.Data;
// Relaciona los modelos de C# con las tablas y la vista existentes en PostgreSQL
// Este mapeo no crea ni modifica el esquema al abrir el contexto
public partial class AppDbContext : DbContext
{
    // Recibe las opciones de conexion configuradas en Program
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Cada DbSet permite consultar y seguir cambios del modelo indicado
    // Las operaciones se ejecutan al consultar o guardar, no al declarar la propiedad
    public virtual DbSet<ArchivosComplementarios> ArchivosComplementarios { get; set; }
    public virtual DbSet<AreasTematicas> AreasTematicas { get; set; }
    public virtual DbSet<AutorizacionesDifusion> AutorizacionesDifusion { get; set; }
    public virtual DbSet<Bitacora> Bitacora { get; set; }
    public virtual DbSet<Carreras> Carreras { get; set; }
    public virtual DbSet<CorreosEventos> CorreosEventos { get; set; }
    public virtual DbSet<CorreosIntentos> CorreosIntentos { get; set; }
    public virtual DbSet<Departamentos> Departamentos { get; set; }
    public virtual DbSet<DistincionesDestacado> DistincionesDestacado { get; set; }
    public virtual DbSet<Entregas> Entregas { get; set; }
    public virtual DbSet<Favoritos> Favoritos { get; set; }
    public virtual DbSet<Notificaciones> Notificaciones { get; set; }
    public virtual DbSet<Observaciones> Observaciones { get; set; }
    public virtual DbSet<Participaciones> Participaciones { get; set; }
    public virtual DbSet<Personas> Personas { get; set; }
    public virtual DbSet<PersonasAutorizaciones> PersonasAutorizaciones { get; set; }
    public virtual DbSet<PlanesEstudio> PlanesEstudio { get; set; }
    public virtual DbSet<PresenciaSesiones> PresenciaSesiones { get; set; }
    public virtual DbSet<ProcedimientosDeposito> ProcedimientosDeposito { get; set; }
    public virtual DbSet<RecuperacionesContrasena> RecuperacionesContrasena { get; set; }
    public virtual DbSet<RecursosDigitales> RecursosDigitales { get; set; }
    public virtual DbSet<Revisiones> Revisiones { get; set; }
    public virtual DbSet<RevisionesDocentes> RevisionesDocentes { get; set; }
    public virtual DbSet<Roles> Roles { get; set; }
    public virtual DbSet<Solicitudes> Solicitudes { get; set; }
    public virtual DbSet<TiposTrabajo> TiposTrabajo { get; set; }
    public virtual DbSet<Trabajos> Trabajos { get; set; }
    public virtual DbSet<TrabajosAreas> TrabajosAreas { get; set; }
    public virtual DbSet<Usuarios> Usuarios { get; set; }
    // Esta propiedad consulta la vista de estrellas y no crea una tabla adicional
    public virtual DbSet<VwEstrellasPorTrabajo> VwEstrellasPorTrabajo { get; set; }

    // Define nombres SQL, claves, limites, valores iniciales y relaciones de los modelos
    // En expresiones como e => e.IdTrabajo, e representa una fila del modelo
    // La parte despues de => selecciona la propiedad que se esta configurando
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Anexos con clave de almacenamiento, propietario y entrega opcional
        modelBuilder.Entity<ArchivosComplementarios>(entity =>
        {
            // HasKey identifica la clave primaria y HasName conserva su nombre SQL
            entity.HasKey(e => e.IdArchivo).HasName("archivos_complementarios_pkey");
            // ToTable indica que tabla corresponde a esta clase
            entity.ToTable("archivos_complementarios");
            // HasIndex describe un indice e IsUnique indica que ese indice es unico
            entity.HasIndex(e => e.Clave, "archivos_complementarios_clave_key").IsUnique();
            // Property selecciona el atributo y HasColumnName indica su columna SQL
            // UseIdentityAlwaysColumn identifica un valor generado por PostgreSQL
            entity.Property(e => e.IdArchivo).UseIdentityAlwaysColumn().HasColumnName("id_archivo");
            // HasMaxLength conserva la longitud configurada para el texto
            entity.Property(e => e.Clave).HasMaxLength(50).HasColumnName("clave");
            entity.Property(e => e.Descripcion).HasMaxLength(1500).HasColumnName("descripcion");
            // HasDefaultValueSql usa una expresion SQL como valor inicial
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("now()").HasColumnName("fecha_registro");
            entity.Property(e => e.Habilitado).HasColumnName("habilitado");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Nombre).HasMaxLength(255).HasColumnName("nombre");
            entity.Property(e => e.TamanoBytes).HasColumnName("tamano_bytes");
            // Un anexo puede apuntar a una entrega y esa entrega puede tener varios anexos
            // HasForeignKey identifica la columna que guarda la referencia
            entity.HasOne(d => d.IdEntregaNavigation).WithMany(p => p.ArchivosComplementarios).HasForeignKey(d => d.IdEntrega).HasConstraintName("archivos_complementarios_id_entrega_fkey");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.ArchivosComplementarios).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("archivos_complementarios_id_trabajo_fkey");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.ArchivosComplementarios).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("archivos_complementarios_id_usuario_fkey");
        });
        // Temas del catalogo con nombre unico y estado activo
        modelBuilder.Entity<AreasTematicas>(entity =>
        {
            entity.HasKey(e => e.IdArea).HasName("pk_areas_tematicas");
            entity.ToTable("areas_tematicas");
            entity.HasIndex(e => e.Nombre, "uq_areas_tematicas_nombre").IsUnique();
            entity.Property(e => e.IdArea).HasColumnName("id_area");
            // HasDefaultValue indica un valor inicial literal, en este caso true
            entity.Property(e => e.Activa).HasDefaultValue(true).HasColumnName("activa");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
        });
        // Constancias por ejemplar con una sola autorizacion Activa a la vez
        modelBuilder.Entity<AutorizacionesDifusion>(entity =>
        {
            entity.HasKey(e => e.IdAutorizacion).HasName("pk_autorizaciones_difusion");
            entity.ToTable("autorizaciones_difusion");
            // El filtro limita la unicidad a constancias Activa y permite conservar su historial
            entity.HasIndex(e => e.IdEntrega, "autorizaciones_una_activa_por_entrega").IsUnique().HasFilter("((estado)::text = 'Activa'::text)");
            entity.Property(e => e.IdAutorizacion).HasColumnName("id_autorizacion");
            entity.Property(e => e.Alcance).HasColumnName("alcance");
            entity.Property(e => e.DescargarPdfConcedido).HasColumnName("descargar_pdf_concedido");
            entity.Property(e => e.DifundirFichaConcedido).HasColumnName("difundir_ficha_concedido");
            entity.Property(e => e.Estado).HasMaxLength(20).HasColumnName("estado");
            entity.Property(e => e.FechaOtorgamiento).HasColumnName("fecha_otorgamiento");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdUsuarioRegistra).HasColumnName("id_usuario_registra");
            entity.Property(e => e.LeerPdfConcedido).HasColumnName("leer_pdf_concedido");
            entity.Property(e => e.ReferenciaRespaldo).HasColumnName("referencia_respaldo");
            entity.Property(e => e.VigenciaHasta).HasColumnName("vigencia_hasta");
            // ClientSetNull afecta relaciones seguidas por EF y no elimina las FK de PostgreSQL
            entity.HasOne(d => d.IdEntregaNavigation).WithMany(p => p.AutorizacionesDifusion).HasForeignKey(d => d.IdEntrega).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_autorizaciones_difusion_entrega");
            entity.HasOne(d => d.IdUsuarioRegistraNavigation).WithMany(p => p.AutorizacionesDifusion).HasForeignKey(d => d.IdUsuarioRegistra).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_autorizaciones_difusion_usuario_registra");
        });
        // Eventos de actividad con su actor y fecha de registro
        modelBuilder.Entity<Bitacora>(entity =>
        {
            entity.HasKey(e => e.IdEvento).HasName("pk_bitacora");
            entity.ToTable("bitacora");
            entity.HasIndex(e => new { e.RegistroAfectado, e.FechaHora }, "bitacora_ultima_suspension").IsDescending(false, true).HasFilter("(((tabla_afectada)::text = 'trabajos'::text) AND ((operacion)::text = 'SUSPENDER_DIFUSION'::text))");
            entity.Property(e => e.IdEvento).HasColumnName("id_evento");
            entity.Property(e => e.ActorTecnico).HasMaxLength(80).HasColumnName("actor_tecnico");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.FechaHora).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_hora");
            entity.Property(e => e.IdUsuarioActor).HasColumnName("id_usuario_actor");
            entity.Property(e => e.Operacion).HasMaxLength(40).HasColumnName("operacion");
            entity.Property(e => e.RegistroAfectado).HasMaxLength(80).HasColumnName("registro_afectado");
            entity.Property(e => e.TablaAfectada).HasMaxLength(60).HasColumnName("tabla_afectada");
            entity.HasOne(d => d.IdUsuarioActorNavigation).WithMany(p => p.Bitacora).HasForeignKey(d => d.IdUsuarioActor).HasConstraintName("fk_bitacora_usuario_actor");
        });
        // Carreras con codigo unico y departamento opcional
        modelBuilder.Entity<Carreras>(entity =>
        {
            entity.HasKey(e => e.IdCarrera).HasName("pk_carreras");
            entity.ToTable("carreras");
            entity.HasIndex(e => e.CodigoCarrera, "uq_carreras_codigo_carrera").IsUnique();
            entity.HasIndex(e => e.Nombre, "uq_carreras_nombre").IsUnique();
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.Activa).HasDefaultValue(true).HasColumnName("activa");
            entity.Property(e => e.CodigoCarrera).HasMaxLength(20).HasColumnName("codigo_carrera");
            entity.Property(e => e.IdDepartamento).HasColumnName("id_departamento");
            entity.Property(e => e.Nombre).HasMaxLength(120).HasColumnName("nombre");
            entity.HasOne(d => d.IdDepartamentoNavigation).WithMany(p => p.Carreras).HasForeignKey(d => d.IdDepartamento).HasConstraintName("carreras_id_departamento_fkey");
        });
        // Cola de avisos con reserva temporal, intentos y ciclos de reenvio
        modelBuilder.Entity<CorreosEventos>(entity =>
        {
            entity.HasKey(e => e.IdCorreo).HasName("correos_eventos_pkey");
            entity.ToTable("correos_eventos");
            entity.HasIndex(e => new { e.IdEvento, e.Destino }, "correos_eventos_id_evento_destino_key").IsUnique();
            entity.HasIndex(e => e.ProximoIntento, "correos_eventos_pendientes").HasFilter("((estado)::text = ANY ((ARRAY['Pendiente'::character varying, 'Enviando'::character varying])::text[]))");
            entity.Property(e => e.IdCorreo).UseIdentityAlwaysColumn().HasColumnName("id_correo");
            entity.Property(e => e.Asunto).HasMaxLength(180).HasColumnName("asunto");
            entity.Property(e => e.BloqueadoHasta).HasColumnName("bloqueado_hasta");
            entity.Property(e => e.Ciclo).HasColumnName("ciclo");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()").HasColumnName("creado_en");
            entity.Property(e => e.Destino).HasMaxLength(254).HasColumnName("destino");
            entity.Property(e => e.EnviadoEn).HasColumnName("enviado_en");
            entity.Property(e => e.ErrorTipo).HasMaxLength(100).HasColumnName("error_tipo");
            entity.Property(e => e.Estado).HasMaxLength(15).HasDefaultValueSql("'Pendiente'::character varying").HasColumnName("estado");
            entity.Property(e => e.IdEvento).HasColumnName("id_evento");
            entity.Property(e => e.Intentos).HasColumnName("intentos");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.ProximoIntento).HasDefaultValueSql("now()").HasColumnName("proximo_intento");
            entity.Property(e => e.Reserva).HasColumnName("reserva");
            entity.Property(e => e.Titulo).HasMaxLength(180).HasColumnName("titulo");
            entity.HasOne(d => d.IdEventoNavigation).WithMany(p => p.CorreosEventos).HasForeignKey(d => d.IdEvento).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("correos_eventos_id_evento_fkey");
        });
        // Historial de intentos de avisos y recuperaciones de cuenta
        modelBuilder.Entity<CorreosIntentos>(entity =>
        {
            entity.HasKey(e => e.IdIntento).HasName("pk_correos_intentos");
            entity.ToTable("correos_intentos");
            entity.HasIndex(e => new { e.IdCorreo, e.Ciclo, e.NumeroIntento }, "uq_correos_intentos_aviso").IsUnique();
            entity.Property(e => e.IdIntento).UseIdentityAlwaysColumn().HasColumnName("id_intento");
            entity.Property(e => e.Categoria).HasMaxLength(15).HasColumnName("categoria");
            entity.Property(e => e.Ciclo).HasColumnName("ciclo");
            entity.Property(e => e.ErrorTipo).HasMaxLength(100).HasColumnName("error_tipo");
            entity.Property(e => e.Fecha).HasDefaultValueSql("clock_timestamp()").HasColumnName("fecha");
            entity.Property(e => e.IdCorreo).HasColumnName("id_correo");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.NumeroIntento).HasColumnName("numero_intento");
            entity.Property(e => e.Resultado).HasMaxLength(15).HasColumnName("resultado");
            entity.HasOne(d => d.IdCorreoNavigation).WithMany(p => p.CorreosIntentos).HasForeignKey(d => d.IdCorreo).HasConstraintName("fk_correos_intentos_correo");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.CorreosIntentos).HasForeignKey(d => d.IdUsuario).HasConstraintName("fk_correos_intentos_usuario");
        });
        // Departamentos academicos con nombre unico
        modelBuilder.Entity<Departamentos>(entity =>
        {
            entity.HasKey(e => e.IdDepartamento).HasName("departamentos_pkey");
            entity.ToTable("departamentos");
            entity.HasIndex(e => e.Nombre, "departamentos_nombre_key").IsUnique();
            entity.Property(e => e.IdDepartamento).HasColumnName("id_departamento");
            entity.Property(e => e.Activa).HasDefaultValue(true).HasColumnName("activa");
            entity.Property(e => e.Nombre).HasMaxLength(100).HasColumnName("nombre");
        });
        // Reconocimientos asociados a un trabajo y a quien los selecciono
        modelBuilder.Entity<DistincionesDestacado>(entity =>
        {
            entity.HasKey(e => e.IdDistincion).HasName("pk_distinciones_destacado");
            entity.ToTable("distinciones_destacado");
            entity.Property(e => e.IdDistincion).HasColumnName("id_distincion");
            entity.Property(e => e.Criterio).HasMaxLength(100).HasColumnName("criterio");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.IdUsuarioSelecciona).HasColumnName("id_usuario_selecciona");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.VigenteDesde).HasColumnName("vigente_desde");
            entity.Property(e => e.VigenteHasta).HasColumnName("vigente_hasta");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.DistincionesDestacado).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_distinciones_destacado_trabajo");
            entity.HasOne(d => d.IdUsuarioSeleccionaNavigation).WithMany(p => p.DistincionesDestacado).HasForeignKey(d => d.IdUsuarioSelecciona).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_distinciones_destacado_usuario_selecciona");
        });
        // Versiones de un trabajo, con permisos y verificacion propios
        modelBuilder.Entity<Entregas>(entity =>
        {
            entity.HasKey(e => e.IdEntrega).HasName("pk_entregas");
            entity.ToTable("entregas");
            entity.HasIndex(e => new { e.IdTrabajo, e.IdEntrega }, "uq_entregas_trabajo_entrega").IsUnique();
            entity.HasIndex(e => new { e.IdTrabajo, e.NumeroVersion }, "uq_entregas_trabajo_numero_version").IsUnique();
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.DescargarPdfHabilitado).HasColumnName("descargar_pdf_habilitado");
            entity.Property(e => e.DifundirFichaHabilitado).HasColumnName("difundir_ficha_habilitado");
            entity.Property(e => e.Estado).HasMaxLength(30).HasDefaultValueSql("'Pendiente'::character varying").HasColumnName("estado");
            entity.Property(e => e.FechaDeposito).HasColumnName("fecha_deposito");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.IdUsuarioEnvia).HasColumnName("id_usuario_envia");
            entity.Property(e => e.LeerPdfHabilitado).HasColumnName("leer_pdf_habilitado");
            entity.Property(e => e.NumeroVersion).HasColumnName("numero_version");
            entity.Property(e => e.ReferenciaConformidad).HasColumnName("referencia_conformidad");
            entity.Property(e => e.VerificadoEn).HasColumnName("verificado_en");
            entity.Property(e => e.VerificadoPor).HasColumnName("verificado_por");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.Entregas).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_entregas_trabajo");
            entity.HasOne(d => d.IdUsuarioEnviaNavigation).WithMany(p => p.EntregasIdUsuarioEnviaNavigation).HasForeignKey(d => d.IdUsuarioEnvia).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_entregas_usuario_envia");
            entity.HasOne(d => d.VerificadoPorNavigation).WithMany(p => p.EntregasVerificadoPorNavigation).HasForeignKey(d => d.VerificadoPor).HasConstraintName("fk_entregas_verificado_por");
        });
        // Una sola pareja usuario y trabajo, con estado activo o inactivo
        modelBuilder.Entity<Favoritos>(entity =>
        {
            entity.HasIndex(e => e.IdTrabajo, "favoritos_estrellas_activas").HasFilter("activo");
            entity.HasKey(e => e.IdFavorito).HasName("pk_favoritos");
            entity.ToTable("favoritos");
            entity.HasIndex(e => new { e.IdUsuario, e.IdTrabajo }, "uq_favoritos_usuario_trabajo").IsUnique();
            entity.Property(e => e.IdFavorito).HasColumnName("id_favorito");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.Favoritos).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_favoritos_trabajo");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Favoritos).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_favoritos_usuario");
        });
        // Avisos internos asociados a un destinatario
        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.HasKey(e => e.IdNotificacion).HasName("pk_notificaciones");
            entity.ToTable("notificaciones");
            entity.Property(e => e.IdNotificacion).HasColumnName("id_notificacion");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaLectura).HasColumnName("fecha_lectura");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Mensaje).HasColumnName("mensaje");
            entity.Property(e => e.TipoEvento).HasMaxLength(40).HasColumnName("tipo_evento");
            entity.HasOne(d => d.IdEntregaNavigation).WithMany(p => p.Notificaciones).HasForeignKey(d => d.IdEntrega).HasConstraintName("fk_notificaciones_entrega");
            entity.HasOne(d => d.IdSolicitudNavigation).WithMany(p => p.Notificaciones).HasForeignKey(d => d.IdSolicitud).HasConstraintName("fk_notificaciones_solicitud");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.Notificaciones).HasForeignKey(d => d.IdTrabajo).HasConstraintName("fk_notificaciones_trabajo");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Notificaciones).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_notificaciones_usuario");
        });
        // Observaciones de una revision con respuesta opcional
        modelBuilder.Entity<Observaciones>(entity =>
        {
            entity.HasKey(e => e.IdObservacion).HasName("pk_observaciones");
            entity.ToTable("observaciones");
            entity.Property(e => e.IdObservacion).HasColumnName("id_observacion");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.EstadoAtencion).HasMaxLength(20).HasColumnName("estado_atencion");
            entity.Property(e => e.FechaObservacion).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_observacion");
            entity.Property(e => e.FechaRespuesta).HasColumnName("fecha_respuesta");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.IdUsuarioResponde).HasColumnName("id_usuario_responde");
            entity.Property(e => e.Respuesta).HasColumnName("respuesta");
            entity.HasOne(d => d.IdRevisionNavigation).WithMany(p => p.Observaciones).HasForeignKey(d => d.IdRevision).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_observaciones_revision");
            entity.HasOne(d => d.IdUsuarioRespondeNavigation).WithMany(p => p.Observaciones).HasForeignKey(d => d.IdUsuarioResponde).HasConstraintName("fk_observaciones_usuario_responde");
        });
        // Personas vinculadas a trabajos, con calidad y orden de autoria
        modelBuilder.Entity<Participaciones>(entity =>
        {
            entity.HasKey(e => e.IdParticipacion).HasName("pk_participaciones");
            entity.ToTable("participaciones");
            entity.HasIndex(e => new { e.IdPersona, e.IdTrabajo, e.CalidadParticipacion }, "uq_participaciones_persona_trabajo_calidad_participacion").IsUnique();
            entity.Property(e => e.IdParticipacion).HasColumnName("id_participacion");
            entity.Property(e => e.CalidadParticipacion).HasMaxLength(30).HasColumnName("calidad_participacion");
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.OrdenAutoria).HasColumnName("orden_autoria");
            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.Participaciones).HasForeignKey(d => d.IdPersona).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_participaciones_persona");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.Participaciones).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_participaciones_trabajo");
        });
        // Datos de personas que pueden participar sin tener una cuenta
        modelBuilder.Entity<Personas>(entity =>
        {
            entity.HasKey(e => e.IdPersona).HasName("pk_personas");
            entity.ToTable("personas");
            entity.HasIndex(e => e.IdentificadorInstitucional, "uq_personas_identificador_institucional").IsUnique();
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.Property(e => e.Apellidos).HasMaxLength(100).HasColumnName("apellidos");
            entity.Property(e => e.IdentificadorInstitucional).HasMaxLength(30).HasColumnName("identificador_institucional");
            entity.Property(e => e.Nombres).HasMaxLength(100).HasColumnName("nombres");
        });
        // Tabla intermedia entre constancias y sus otorgantes
        modelBuilder.Entity<PersonasAutorizaciones>(entity =>
        {
            entity.HasKey(e => e.IdOtorgante).HasName("pk_personas_autorizaciones");
            entity.ToTable("personas_autorizaciones");
            entity.HasIndex(e => new { e.IdPersona, e.IdAutorizacion }, "uq_personas_autorizaciones_persona_autorizacion").IsUnique();
            entity.Property(e => e.IdOtorgante).HasColumnName("id_otorgante");
            entity.Property(e => e.IdAutorizacion).HasColumnName("id_autorizacion");
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.HasOne(d => d.IdAutorizacionNavigation).WithMany(p => p.PersonasAutorizaciones).HasForeignKey(d => d.IdAutorizacion).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_personas_autorizaciones_autorizacion");
            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.PersonasAutorizaciones).HasForeignKey(d => d.IdPersona).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_personas_autorizaciones_persona");
        });
        // Versiones del pensum que pertenecen a una carrera
        modelBuilder.Entity<PlanesEstudio>(entity =>
        {
            entity.HasKey(e => e.IdPlan).HasName("planes_estudio_pkey");
            entity.ToTable("planes_estudio");
            entity.HasIndex(e => new { e.IdCarrera, e.Nombre }, "planes_estudio_id_carrera_nombre_key").IsUnique();
            entity.Property(e => e.IdPlan).HasColumnName("id_plan");
            entity.Property(e => e.Activa).HasDefaultValue(true).HasColumnName("activa");
            entity.Property(e => e.DocumentoUrl).HasColumnName("documento_url");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.Nombre).HasMaxLength(120).HasColumnName("nombre");
            entity.HasOne(d => d.IdCarreraNavigation).WithMany(p => p.PlanesEstudio).HasForeignKey(d => d.IdCarrera).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("planes_estudio_id_carrera_fkey");
        });
        // Sesiones identificadas por GUID y su ultima actividad
        modelBuilder.Entity<PresenciaSesiones>(entity =>
        {
            entity.HasKey(e => e.IdSesion).HasName("presencia_sesiones_pkey");
            entity.ToTable("presencia_sesiones");
            entity.HasIndex(e => e.UltimaActividad, "ix_presencia_actividad");
            entity.Property(e => e.IdSesion).ValueGeneratedNever().HasColumnName("id_sesion");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.UltimaActividad).HasDefaultValueSql("now()").HasColumnName("ultima_actividad");
            entity.Property(e => e.VersionSesion).HasColumnName("version_sesion");
            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.PresenciaSesiones).HasForeignKey(d => d.IdUsuario).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("presencia_sesiones_id_usuario_fkey");
        });
        // Versiones documentadas de los procedimientos y sus fechas de vigencia
        modelBuilder.Entity<ProcedimientosDeposito>(entity =>
        {
            entity.HasKey(e => e.IdProcedimiento).HasName("pk_procedimientos_deposito");
            entity.ToTable("procedimientos_deposito");
            entity.HasIndex(e => e.VersionProcedimiento, "uq_procedimientos_deposito_version_procedimiento").IsUnique();
            entity.Property(e => e.IdProcedimiento).HasColumnName("id_procedimiento");
            entity.Property(e => e.FechaAprobacion).HasColumnName("fecha_aprobacion");
            entity.Property(e => e.ReferenciaDocumento).HasColumnName("referencia_documento");
            entity.Property(e => e.VersionProcedimiento).HasMaxLength(20).HasColumnName("version_procedimiento");
            entity.Property(e => e.VigenteDesde).HasColumnName("vigente_desde");
            entity.Property(e => e.VigenteHasta).HasColumnName("vigente_hasta");
        });
        // Un registro de recuperacion por usuario, usando su ID como PK y FK
        modelBuilder.Entity<RecuperacionesContrasena>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("recuperaciones_contrasena_pkey");
            entity.ToTable("recuperaciones_contrasena");
            entity.Property(e => e.IdUsuario).ValueGeneratedNever().HasColumnName("id_usuario");
            entity.Property(e => e.CodigoProtegido).HasColumnName("codigo_protegido");
            entity.Property(e => e.EnviosVentana).HasDefaultValue(1).HasColumnName("envios_ventana");
            entity.Property(e => e.InicioVentana).HasColumnName("inicio_ventana");
            entity.Property(e => e.Intentos).HasColumnName("intentos");
            entity.Property(e => e.UltimoEnvio).HasColumnName("ultimo_envio");
            entity.Property(e => e.VenceEn).HasColumnName("vence_en");
            entity.HasOne(d => d.IdUsuarioNavigation).WithOne(p => p.RecuperacionesContrasena).HasForeignKey<RecuperacionesContrasena>(d => d.IdUsuario).HasConstraintName("recuperaciones_contrasena_id_usuario_fkey");
        });
        // Referencias a PDF, enlaces y comprobantes UES de los trabajos
        modelBuilder.Entity<RecursosDigitales>(entity =>
        {
            entity.HasKey(e => e.IdRecurso).HasName("pk_recursos_digitales");
            entity.ToTable("recursos_digitales");
            entity.HasIndex(e => e.IdEntrega, "recursos_un_principal_por_entrega").IsUnique().HasFilter("((clase)::text = ANY ((ARRAY['PDF'::character varying, 'REF_UES'::character varying])::text[]))");
            entity.Property(e => e.IdRecurso).HasColumnName("id_recurso");
            entity.Property(e => e.Clase).HasMaxLength(10).HasColumnName("clase");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.DifusionHabilitada).HasColumnName("difusion_habilitada");
            entity.Property(e => e.EstadoCarga).HasMaxLength(20).HasColumnName("estado_carga");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.Nombre).HasMaxLength(255).HasColumnName("nombre");
            entity.Property(e => e.TamanoBytes).HasColumnName("tamano_bytes");
            entity.Property(e => e.TipoFormato).HasMaxLength(100).HasColumnName("tipo_formato");
            entity.Property(e => e.Ubicacion).HasColumnName("ubicacion");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.RecursosDigitales).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_recursos_digitales_trabajo");
            entity.HasOne(d => d.Entregas).WithMany(p => p.RecursosDigitales).HasPrincipalKey(p => new { p.IdTrabajo, p.IdEntrega }).HasForeignKey(d => new { d.IdTrabajo, d.IdEntrega }).HasConstraintName("fk_recursos_digitales_entrega_mismo_trabajo");
        });
        // Revisiones de un ejemplar con ambito, responsable y resultado
        modelBuilder.Entity<Revisiones>(entity =>
        {
            entity.HasKey(e => e.IdRevision).HasName("pk_revisiones");
            entity.ToTable("revisiones");
            entity.HasIndex(e => new { e.IdEntrega, e.FechaCierre, e.IdRevision }, "revisiones_docente_ultima").IsDescending(false, true, true).HasFilter("(((ambito_revision)::text = 'Revisión docente'::text) AND (fecha_cierre IS NOT NULL))");
            entity.HasIndex(e => new { e.IdEntrega, e.FechaCierre, e.IdRevision }, "revisiones_ultima_decision_difusion").IsDescending(false, true, true).HasFilter("(((ambito_revision)::text = 'Difusión'::text) AND (fecha_cierre IS NOT NULL))");
            entity.Property(e => e.IdRevision).HasColumnName("id_revision");
            entity.Property(e => e.AmbitoRevision).HasMaxLength(20).HasColumnName("ambito_revision");
            entity.Property(e => e.FechaCierre).HasColumnName("fecha_cierre");
            entity.Property(e => e.FechaInicio).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_inicio");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdProcedimiento).HasColumnName("id_procedimiento");
            entity.Property(e => e.IdUsuarioRevisor).HasColumnName("id_usuario_revisor");
            entity.Property(e => e.MotivoDecision).HasColumnName("motivo_decision");
            entity.Property(e => e.Resultado).HasMaxLength(30).HasColumnName("resultado");
            entity.HasOne(d => d.IdEntregaNavigation).WithMany(p => p.Revisiones).HasForeignKey(d => d.IdEntrega).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_revisiones_entrega");
            entity.HasOne(d => d.IdProcedimientoNavigation).WithMany(p => p.Revisiones).HasForeignKey(d => d.IdProcedimiento).HasConstraintName("fk_revisiones_procedimiento");
            entity.HasOne(d => d.IdUsuarioRevisorNavigation).WithMany(p => p.Revisiones).HasForeignKey(d => d.IdUsuarioRevisor).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_revisiones_usuario_revisor");
        });
        // Mapeo de la tabla docente conservada como historial anterior
        modelBuilder.Entity<RevisionesDocentes>(entity =>
        {
            entity.HasKey(e => e.IdRevision).HasName("revisiones_docentes_pkey");
            entity.ToTable("revisiones_docentes");
            entity.HasIndex(e => new { e.IdTrabajo, e.IdRevision }, "revisiones_docentes_trabajo").IsDescending(false, true);
            entity.Property(e => e.IdRevision).UseIdentityAlwaysColumn().HasColumnName("id_revision");
            entity.Property(e => e.CreadoEn).HasDefaultValueSql("now()").HasColumnName("creado_en");
            entity.Property(e => e.IdDocente).HasColumnName("id_docente");
            entity.Property(e => e.IdEntrega).HasColumnName("id_entrega");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.Resultado).HasMaxLength(30).HasColumnName("resultado");
            entity.HasOne(d => d.IdDocenteNavigation).WithMany(p => p.RevisionesDocentes).HasForeignKey(d => d.IdDocente).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("revisiones_docentes_id_docente_fkey");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.RevisionesDocentes).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("revisiones_docentes_id_trabajo_fkey");
            entity.HasOne(d => d.Entregas).WithMany(p => p.RevisionesDocentes).HasPrincipalKey(p => new { p.IdTrabajo, p.IdEntrega }).HasForeignKey(d => new { d.IdTrabajo, d.IdEntrega }).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("revisiones_docentes_id_trabajo_id_entrega_fkey");
        });
        // Roles disponibles para las cuentas de usuario
        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasKey(e => e.IdRol).HasName("pk_roles");
            entity.ToTable("roles");
            entity.HasIndex(e => e.Nombre, "uq_roles_nombre").IsUnique();
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.Activo).HasDefaultValue(true).HasColumnName("activo");
            entity.Property(e => e.Nombre).HasMaxLength(50).HasColumnName("nombre");
        });
        // Solicitudes conservadas para el tramite academico previsto
        modelBuilder.Entity<Solicitudes>(entity =>
        {
            entity.HasKey(e => e.IdSolicitud).HasName("pk_solicitudes");
            entity.ToTable("solicitudes");
            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Estado).HasMaxLength(30).HasColumnName("estado");
            entity.Property(e => e.FechaPresentacion).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_presentacion");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.IdTipoTrabajo).HasColumnName("id_tipo_trabajo");
            entity.Property(e => e.IdUsuarioSolicitante).HasColumnName("id_usuario_solicitante");
            entity.Property(e => e.PlazoCorreccion).HasColumnName("plazo_correccion");
            entity.Property(e => e.TituloProvisional).HasMaxLength(250).HasColumnName("titulo_provisional");
            entity.HasOne(d => d.IdCarreraNavigation).WithMany(p => p.Solicitudes).HasForeignKey(d => d.IdCarrera).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_solicitudes_carrera");
            entity.HasOne(d => d.IdTipoTrabajoNavigation).WithMany(p => p.Solicitudes).HasForeignKey(d => d.IdTipoTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_solicitudes_tipo_trabajo");
            entity.HasOne(d => d.IdUsuarioSolicitanteNavigation).WithMany(p => p.Solicitudes).HasForeignKey(d => d.IdUsuarioSolicitante).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_solicitudes_usuario_solicitante");
        });
        // Tipos del catalogo y su requisito de aprobacion academica
        modelBuilder.Entity<TiposTrabajo>(entity =>
        {
            entity.HasKey(e => e.IdTipoTrabajo).HasName("pk_tipos_trabajo");
            entity.ToTable("tipos_trabajo");
            entity.HasIndex(e => e.Nombre, "uq_tipos_trabajo_nombre").IsUnique();
            entity.Property(e => e.IdTipoTrabajo).HasColumnName("id_tipo_trabajo");
            entity.Property(e => e.Activa).HasDefaultValue(true).HasColumnName("activa");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Nombre).HasMaxLength(60).HasColumnName("nombre");

        });
        // Ficha academica y referencia compuesta a su ejemplar final
        modelBuilder.Entity<Trabajos>(entity =>
        {
            entity.HasKey(e => e.IdTrabajo).HasName("pk_trabajos");
            entity.ToTable("trabajos");
            entity.HasIndex(e => e.IdUsuarioEnvio, "ix_trabajos_usuario_envio").HasFilter("(id_usuario_envio IS NOT NULL)");
            entity.HasIndex(e => e.Codigo, "uq_trabajos_codigo").IsUnique();
            entity.HasIndex(e => e.IdEntregaFinal, "uq_trabajos_entrega_final").IsUnique();
            entity.HasIndex(e => e.IdSolicitud, "uq_trabajos_solicitud").IsUnique();
            entity.Property(e => e.IdTrabajo).ValueGeneratedOnAdd().HasColumnName("id_trabajo");
            entity.Property(e => e.Anio).HasColumnName("anio");
            entity.Property(e => e.Codigo).HasMaxLength(30).HasColumnName("codigo");
            entity.Property(e => e.DepositoVerificadoEn).HasColumnName("deposito_verificado_en");
            entity.Property(e => e.DepositoVerificadoPor).HasColumnName("deposito_verificado_por");
            entity.Property(e => e.EstadoPublicacion).HasMaxLength(20).HasDefaultValueSql("'Borrador'::character varying").HasColumnName("estado_publicacion");
            entity.Property(e => e.FechaPublicacion).HasColumnName("fecha_publicacion");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_registro");
            entity.Property(e => e.FechaRetiro).HasColumnName("fecha_retiro");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.IdEntregaFinal).HasColumnName("id_entrega_final");
            entity.Property(e => e.IdSolicitud).HasColumnName("id_solicitud");
            entity.Property(e => e.IdTipoTrabajo).HasColumnName("id_tipo_trabajo");
            entity.Property(e => e.IdUsuarioEnvio).HasColumnName("id_usuario_envio");
            entity.Property(e => e.IdUsuarioRetira).HasColumnName("id_usuario_retira");
            entity.Property(e => e.MotivoRetiro).HasColumnName("motivo_retiro");
            entity.Property(e => e.ReferenciaDepositoUes).HasColumnName("referencia_deposito_ues");
            entity.Property(e => e.Resumen).HasColumnName("resumen");
            entity.Property(e => e.Titulo).HasMaxLength(250).HasColumnName("titulo");
            entity.HasOne(d => d.DepositoVerificadoPorNavigation).WithMany(p => p.TrabajosDepositoVerificadoPorNavigation).HasForeignKey(d => d.DepositoVerificadoPor).HasConstraintName("fk_trabajos_deposito_verificado_por");
            entity.HasOne(d => d.IdCarreraNavigation).WithMany(p => p.Trabajos).HasForeignKey(d => d.IdCarrera).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_trabajos_carrera");
            entity.HasOne(d => d.IdSolicitudNavigation).WithOne(p => p.Trabajos).HasForeignKey<Trabajos>(d => d.IdSolicitud).HasConstraintName("fk_trabajos_solicitud");
            entity.HasOne(d => d.IdTipoTrabajoNavigation).WithMany(p => p.Trabajos).HasForeignKey(d => d.IdTipoTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_trabajos_tipo_trabajo");
            entity.HasOne(d => d.IdUsuarioEnvioNavigation).WithMany(p => p.TrabajosIdUsuarioEnvioNavigation).HasForeignKey(d => d.IdUsuarioEnvio).HasConstraintName("trabajos_id_usuario_envio_fkey");
            entity.HasOne(d => d.IdUsuarioRetiraNavigation).WithMany(p => p.TrabajosIdUsuarioRetiraNavigation).HasForeignKey(d => d.IdUsuarioRetira).HasConstraintName("fk_trabajos_usuario_retira");
            // La clave compuesta comprueba que el ejemplar final pertenezca a este trabajo
            // new { ... } agrupa las propiedades que forman esa clave
            entity.HasOne(d => d.EntregasNavigation).WithMany(p => p.Trabajos).HasPrincipalKey(p => new { p.IdTrabajo, p.IdEntrega }).HasForeignKey(d => new { d.IdTrabajo, d.IdEntregaFinal }).HasConstraintName("fk_trabajos_entrega_final");
        });
        // Tabla intermedia para la relacion entre trabajos y temas
        modelBuilder.Entity<TrabajosAreas>(entity =>
        {
            entity.HasKey(e => e.IdTrabajoArea).HasName("pk_trabajos_areas");
            entity.ToTable("trabajos_areas");
            entity.HasIndex(e => new { e.IdTrabajo, e.IdArea }, "uq_trabajos_areas_trabajo_area").IsUnique();
            entity.Property(e => e.IdTrabajoArea).HasColumnName("id_trabajo_area");
            entity.Property(e => e.IdArea).HasColumnName("id_area");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
            entity.HasOne(d => d.IdAreaNavigation).WithMany(p => p.TrabajosAreas).HasForeignKey(d => d.IdArea).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_trabajos_areas_area");
            entity.HasOne(d => d.IdTrabajoNavigation).WithMany(p => p.TrabajosAreas).HasForeignKey(d => d.IdTrabajo).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_trabajos_areas_trabajo");
        });
        // Cuenta, perfil y relaciones con persona, rol, carrera, plan y docente
        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("pk_usuarios");
            entity.ToTable("usuarios");
            entity.HasIndex(e => e.CorreoInstitucional, "uq_usuarios_correo_institucional").IsUnique();
            entity.HasIndex(e => e.IdPersona, "uq_usuarios_persona").IsUnique();
            entity.HasIndex(e => e.IdDocente, "usuarios_docente").HasFilter("(id_docente IS NOT NULL)");
            entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
            entity.Property(e => e.Avatar).HasMaxLength(12).HasDefaultValueSql("'turquesa'::character varying").HasColumnName("avatar");
            entity.Property(e => e.CorreoInstitucional).HasMaxLength(150).HasColumnName("correo_institucional");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValueSql("'Habilitada'::character varying").HasColumnName("estado");
            entity.Property(e => e.FechaAlta).HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("fecha_alta");
            entity.Property(e => e.FotoPerfil).HasColumnName("foto_perfil");
            entity.Property(e => e.FotoTipo).HasMaxLength(30).HasColumnName("foto_tipo");
            entity.Property(e => e.FotoVersion).HasColumnName("foto_version");
            entity.Property(e => e.HashContrasena).HasColumnName("hash_contrasena");
            entity.Property(e => e.IdCarrera).HasColumnName("id_carrera");
            entity.Property(e => e.IdDocente).HasColumnName("id_docente");
            entity.Property(e => e.IdPersona).HasColumnName("id_persona");
            entity.Property(e => e.IdPlan).HasColumnName("id_plan");
            entity.Property(e => e.IdRol).HasColumnName("id_rol");
            entity.Property(e => e.NombreMostrar).HasMaxLength(120).HasColumnName("nombre_mostrar");
            entity.Property(e => e.ProveedorIdentidad).HasMaxLength(50).HasColumnName("proveedor_identidad");
            entity.Property(e => e.Tema).HasMaxLength(10).HasDefaultValueSql("'sistema'::character varying").HasColumnName("tema");
            entity.Property(e => e.VersionRol).HasColumnName("version_rol");
            entity.HasOne(d => d.IdCarreraNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdCarrera).HasConstraintName("usuarios_id_carrera_fkey");
            // Relacion de Usuarios consigo misma: un docente puede tener varios estudiantes
            entity.HasOne(d => d.IdDocenteNavigation).WithMany(p => p.InverseIdDocenteNavigation).HasForeignKey(d => d.IdDocente).HasConstraintName("usuarios_id_docente_fkey");
            // WithOne describe una cuenta por persona, no varias cuentas para la misma persona
            entity.HasOne(d => d.IdPersonaNavigation).WithOne(p => p.Usuarios).HasForeignKey<Usuarios>(d => d.IdPersona).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_usuarios_persona");
            entity.HasOne(d => d.IdPlanNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdPlan).HasConstraintName("usuarios_id_plan_fkey");
            entity.HasOne(d => d.IdRolNavigation).WithMany(p => p.Usuarios).HasForeignKey(d => d.IdRol).OnDelete(DeleteBehavior.ClientSetNull).HasConstraintName("fk_usuarios_rol");
        });
        // Resultado de consulta del conteo de estrellas, sin clave de entidad
        modelBuilder.Entity<VwEstrellasPorTrabajo>(entity =>
        {
            // HasNoKey indica que el resultado no tiene una PK definida en EF
            // ToView lo vincula a la vista usada para consultar el conteo de favoritos
            entity.HasNoKey().ToView("vw_estrellas_por_trabajo");
            entity.Property(e => e.Estrellas).HasColumnName("estrellas");
            entity.Property(e => e.IdTrabajo).HasColumnName("id_trabajo");
        });
        // Permite ampliar el mapeo desde otra parte de esta clase parcial
        OnModelCreatingPartial(modelBuilder);
    }

    // Punto de extension opcional del mapeo
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
