import {Component,inject,signal} from '@angular/core';import {Router,RouterLink,RouterLinkActive,RouterOutlet} from '@angular/router';import {Api} from './core/api';
@Component({selector:'app-root',imports:[RouterOutlet,RouterLink,RouterLinkActive],templateUrl:'./app.html',styleUrl:'./app.css'})
export class App {api=inject(Api);router=inject(Router);error=signal('');
async logout(){try{await this.api.logout();await this.router.navigateByUrl('/');}catch(e:any){this.error.set(e.message);}}}
