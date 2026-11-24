using System.Collections.Concurrent;
using System.Threading.Channels;

namespace APIScriptoriumArchivum.Datos;

public sealed class CambiosEnVivo
{
    private readonly ConcurrentDictionary<Guid,Channel<string[]>> clientes = new();
    private readonly ConcurrentDictionary<string,long> versiones = new();
    private readonly string inicio = Guid.NewGuid().ToString("N");
    public object Estado()=>new {inicio,versiones=versiones.ToDictionary(x=>x.Key,x=>x.Value)};
    public (Guid Id,ChannelReader<string[]> Lector) Suscribir()
    {
        var id=Guid.NewGuid();
        var canal=Channel.CreateBounded<string[]>(new BoundedChannelOptions(32){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true});
        clientes[id]=canal;
        return (id,canal.Reader);
    }
    public void Quitar(Guid id){if(clientes.TryRemove(id,out var canal))canal.Writer.TryComplete();}
    public void Publicar(params string[] areas){
        foreach(var area in areas)versiones.AddOrUpdate(area,1,(_,valor)=>valor+1);
        foreach(var canal in clientes.Values)canal.Writer.TryWrite(areas);
    }
    public void Peticion(string ruta)
    {
        if(ruta.StartsWith("/api/presencia")||ruta is "/api/auth/login" or "/api/auth/logout") Publicar("presencia");
        else if(ruta.StartsWith("/api/admin/roles")&&!ruta.Contains("recuperacion")) Publicar("cuentas","asignacion","docente","perfil","presencia","correos");
        else if(ruta.StartsWith("/api/auth/perfil")||ruta.StartsWith("/api/auth/foto")||ruta=="/api/auth/recuperacion/confirmar")Publicar("perfil","cuentas");
        else if(ruta.StartsWith("/api/favoritos"))Publicar("catalogo","favoritos","ficha");
        else if(ruta.StartsWith("/api/admin/catalogos"))Publicar("catalogos","catalogo","biblioteca","perfil");
        else if(ruta.StartsWith("/api/admin/trabajos")||ruta.StartsWith("/api/mis-envios")||ruta.StartsWith("/api/docente/trabajos")||ruta.StartsWith("/api/trabajos"))
            Publicar("biblioteca","envios","docente","catalogo","ficha","revisiones");
    }
}
