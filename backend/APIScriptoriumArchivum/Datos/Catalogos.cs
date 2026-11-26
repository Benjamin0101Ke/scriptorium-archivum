using APIScriptoriumArchivum.Data;
using APIScriptoriumArchivum.Dtos;
using Microsoft.EntityFrameworkCore;

namespace APIScriptoriumArchivum.Datos
{
    // Consulta los catalogos disponibles para los formularios
    public class Catalogos
    {
        private AppDbContext contexto;
        public Catalogos(AppDbContext contexto)
        {
            this.contexto = contexto;
        }

        // Obtiene las carreras activas con el contrato de la interfaz
        public async Task<List<CarreraDto>> ListarCarrerasAsync(CancellationToken cancellationToken)
        {
            return await contexto.Carreras.AsNoTracking().Where(c => c.Activa).OrderBy(c => c.Nombre).ThenBy(c => c.IdCarrera).Select(c => new CarreraDto(c.IdCarrera, c.Nombre)).ToListAsync(cancellationToken);
        }

        // Obtiene los tipos habilitados para registrar trabajos
        public async Task<List<TipoDeTrabajoDto>> ListarTiposDeTrabajoAsync(CancellationToken cancellationToken)
        {
            return await contexto.TiposTrabajo.AsNoTracking().Where(t => t.Activa).OrderBy(t => t.Nombre).ThenBy(t => t.IdTipoTrabajo).Select(t => new TipoDeTrabajoDto(t.IdTipoTrabajo, t.Nombre)).ToListAsync(cancellationToken);
        }

        // Obtiene las areas tematicas activas
        public async Task<List<AreaTematicaDto>> ListarAreasTematicasAsync(CancellationToken cancellationToken)
        {
            return await contexto.AreasTematicas.AsNoTracking().Where(a => a.Activa).OrderBy(a => a.Nombre).ThenBy(a => a.IdArea).Select(a => new AreaTematicaDto(a.IdArea, a.Nombre)).ToListAsync(cancellationToken);
        }
    }
}
