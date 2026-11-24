import {Component,input} from '@angular/core';

// Original Scriptorium line drawings, kept as vectors for crisp rendering at any size.
@Component({selector:'app-icono-rol',template:`
<svg viewBox="0 0 64 64" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
@switch(nombre()){
 @case('admin') {<path d="M32 6 52 14v16c0 13-10 22-20 28C22 52 12 43 12 30V14Z"/><circle cx="30" cy="28" r="6"/><path d="m34 33 9 9m-4-4 4-4m-7 1 4-4M23 48l9 5"/>}
 @case('biblioteca') {<path d="M8 22h48v32H8Zm5-8h38v8M19 8h26v6M8 32h48M26 39h12v7H26Z"/><path d="M17 27h4m22 0h4M32 14V8"/>}
 @case('docente') {<path d="M10 13h31v42H10Zm7 9h15m-15 8h9m-9 17h12M35 40l-2 10 10-3L57 23l-8-5Z"/><path d="m45 25 8 5M35 40l8 7"/>}
 @case('estudiante') {<path d="m5 23 27-13 27 13-27 13Zm11 7v14c8 9 24 9 32 0V30M56 25v18m-3 5h6l-3-5Z"/><path d="M25 54h14"/>}
 @case('lector') {<path d="M32 19c-8-6-17-7-25-3v36c9-4 17-3 25 2 8-5 16-6 25-2V16c-8-4-17-3-25 3Zm0 0v35M14 24l10 3m-10 5 10 3m16-8 10-3m-10 11 10-3"/>}
 @case('catalogos') {<rect x="7" y="7" width="20" height="20" rx="5"/><rect x="37" y="7" width="20" height="20" rx="5"/><rect x="7" y="37" width="20" height="20" rx="5"/><path d="M47 36v22M36 47h22M13 17h8m26-6v12M13 47h8"/>}
 @default {<path d="m32 16 16 7v11c0 10-8 17-16 22-8-5-16-12-16-22V23Z"/><path d="m23 31 6 6 13-13M23 11l-3-7 12 4 12-4-3 7"/><path d="M8 19C-3 37 10 62 30 61M56 46C68 27 56 7 43 4"/><circle cx="8" cy="19" r="3"/><circle cx="56" cy="46" r="3"/>}
}
</svg>`,styles:`:host{display:inline-flex;width:64px;height:64px;flex-shrink:0}svg{width:100%;height:100%;overflow:visible}`})
export class IconoRol {nombre=input('superadmin');}
