const niveles = [
  { minimo: 5, clase: 'rating-gold' },
  { minimo: 4.5, clase: 'rating-silver' },
  { minimo: 4, clase: 'rating-bronze' },
  { minimo: 3, clase: 'rating-teal' },
  { minimo: 2, clase: 'rating-violet' },
  { minimo: 1, clase: 'rating-coral' },
];

export function claseValoracion(media = 0, total = 0): string {
  if (!total) return '';
  return niveles.find((nivel) => media >= nivel.minimo)?.clase ?? '';
}
