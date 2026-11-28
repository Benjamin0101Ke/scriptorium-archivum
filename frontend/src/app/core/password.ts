/** Indicador orientativo. Solo la longitud condiciona el registro. */
export function evaluarPassword(password: string) {
  const longitud = password.length >= 8 && password.length <= 512;
  const simbolos = /[^\p{L}\p{N}\s]/u.test(password);
  const mayusculasONumeros = /[\p{Lu}\p{N}]/u.test(password);
  const puntos = Number(longitud) + Number(simbolos) + Number(mayusculasONumeros);
  const etiqueta = !password ? 'Sin evaluar' : !longitud ? 'Longitud insuficiente o excesiva'
    : puntos === 3 ? 'Alta' : puntos === 2 ? 'Media' : 'Baja';
  return { longitud, simbolos, mayusculasONumeros, puntos, etiqueta };
}
