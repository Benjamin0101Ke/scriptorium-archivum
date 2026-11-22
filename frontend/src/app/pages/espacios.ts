import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({ selector: 'app-lector', imports: [RouterLink], templateUrl: './lector.html' })
export class Lector {}

@Component({ selector: 'app-estudiante', imports: [RouterLink], templateUrl: './estudiante.html' })
export class Estudiante {}
