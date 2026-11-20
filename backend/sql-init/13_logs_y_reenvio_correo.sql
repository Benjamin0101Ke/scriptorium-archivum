BEGIN;
ALTER TABLE public.correos_eventos
    ADD COLUMN IF NOT EXISTS ciclo integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS reserva uuid;

CREATE TABLE IF NOT EXISTS public.correos_intentos (
    id_intento bigint GENERATED ALWAYS AS IDENTITY,
    id_correo bigint,
    id_usuario integer,
    categoria varchar(15) NOT NULL,
    ciclo integer NOT NULL DEFAULT 0,
    numero_intento integer NOT NULL,
    resultado varchar(15) NOT NULL,
    fecha timestamptz NOT NULL DEFAULT clock_timestamp(),
    error_tipo varchar(100),
    CONSTRAINT pk_correos_intentos PRIMARY KEY (id_intento),
    CONSTRAINT fk_correos_intentos_correo FOREIGN KEY (id_correo)
        REFERENCES public.correos_eventos (id_correo),
    CONSTRAINT fk_correos_intentos_usuario FOREIGN KEY (id_usuario)
        REFERENCES public.usuarios (id_usuario),
    CONSTRAINT uq_correos_intentos_aviso UNIQUE (id_correo, ciclo, numero_intento),
    CONSTRAINT ck_correos_intentos_numero CHECK (numero_intento BETWEEN 1 AND 5),
    CONSTRAINT ck_correos_intentos_ciclo CHECK (ciclo >= 0),
    CONSTRAINT ck_correos_intentos_resultado CHECK (resultado IN ('Enviado', 'Fallido', 'Omitido')),
    CONSTRAINT ck_correos_intentos_categoria CHECK (
        (categoria = 'Aviso' AND id_correo IS NOT NULL AND id_usuario IS NULL)
        OR (categoria = 'Recuperacion' AND id_correo IS NULL AND id_usuario IS NOT NULL)
    )
);
COMMIT;
