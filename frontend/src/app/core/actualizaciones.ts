import {DestroyRef,Injectable,signal} from '@angular/core';
import {settings} from './settings';
@Injectable({providedIn:'root'})
export class Actualizaciones {
  conectado=signal(false);
  private listeners=new Set<{areas:string[];callback:()=>void|Promise<void>}>();
  private pendientes=new Set<string>();
  private timer?:ReturnType<typeof setTimeout>;
  private ultimoEvento=0;
  private consultando=false;
  private estado?:{inicio:string;versiones:Record<string,number>};
  constructor(){
    if(settings.demo||typeof EventSource==='undefined')return;
    const stream=new EventSource(settings.apiUrl+'/cambios',{withCredentials:true});
    stream.onopen=()=>this.conectado.set(true);
    stream.onerror=()=>this.conectado.set(false);
    stream.onmessage=event=>{try{const areas=JSON.parse(event.data);if(!Array.isArray(areas))return;this.ultimoEvento=Date.now();for(const area of areas)this.pendientes.add(area);
      clearTimeout(this.timer);this.timer=setTimeout(()=>this.notificar(),180);
    }catch{/* Un evento incompleto se recupera al reconectar. */}};
    window.addEventListener('online',()=>{this.pendientes.add('todo');this.notificar();});
    // Some public tunnels buffer event streams. A small version check keeps them current.
    setInterval(()=>void this.comprobar(),3000);
    document.addEventListener('visibilitychange',()=>{if(!document.hidden)void this.comprobar();});
    void this.comprobar();
  }
  private async comprobar(){
    if(document.hidden||!navigator.onLine||this.consultando)return;
    this.consultando=true;
    try{
      const response=await fetch(settings.apiUrl+'/cambios/estado',{credentials:'include',cache:'no-store',signal:AbortSignal.timeout(8000)});
      if(!response.ok)return;
      const actual=await response.json() as {inicio:string;versiones:Record<string,number>};
      if(!actual.inicio||!actual.versiones)return;
      const anterior=this.estado;this.estado=actual;
      if(Date.now()-this.ultimoEvento<8000)return;
      if(!anterior||anterior.inicio!==actual.inicio)this.pendientes.add('todo');
      else for(const [area,version] of Object.entries(actual.versiones))if(anterior.versiones[area]!==version)this.pendientes.add(area);
      if(this.pendientes.size)this.notificar();
    }catch{/* The next check retries after connectivity returns. */}
    finally{this.consultando=false;}
  }
  watch(areas:string[],callback:()=>void|Promise<void>,destroy:DestroyRef){
    const listener={areas,callback};this.listeners.add(listener);destroy.onDestroy(()=>this.listeners.delete(listener));
  }
  private notificar(){const areas=new Set(this.pendientes);this.pendientes.clear();
    for(const listener of this.listeners)if(areas.has('todo')||listener.areas.some(area=>areas.has(area)))
      void Promise.resolve().then(listener.callback).catch(()=>undefined);
  }
}
