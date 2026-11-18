BEGIN;

ALTER TABLE public.usuarios ADD COLUMN IF NOT EXISTS id_docente integer
    REFERENCES public.usuarios(id_usuario);
CREATE INDEX IF NOT EXISTS usuarios_docente ON public.usuarios(id_docente) WHERE id_docente IS NOT NULL;

-- Cola durable: contiene avisos, nunca códigos de recuperación ni contraseñas.
CREATE TABLE IF NOT EXISTS public.correos_eventos (
    id_correo bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_evento integer NOT NULL REFERENCES public.bitacora(id_evento),
    destino varchar(254) NOT NULL,
    asunto varchar(180) NOT NULL,
    titulo varchar(180) NOT NULL,
    mensaje text NOT NULL,
    estado varchar(15) NOT NULL DEFAULT 'Pendiente'
        CHECK (estado IN ('Pendiente','Enviando','Enviado','Fallido','Omitido')),
    intentos integer NOT NULL DEFAULT 0 CHECK (intentos BETWEEN 0 AND 5),
    creado_en timestamptz NOT NULL DEFAULT now(),
    proximo_intento timestamptz NOT NULL DEFAULT now(),
    bloqueado_hasta timestamptz,
    enviado_en timestamptz,
    error_tipo varchar(100),
    UNIQUE (id_evento, destino)
);
CREATE INDEX IF NOT EXISTS correos_eventos_pendientes ON public.correos_eventos(proximo_intento)
    WHERE estado IN ('Pendiente','Enviando');

COMMIT;
