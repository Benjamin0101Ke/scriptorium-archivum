-- Revisiones docentes: Conforme / Requiere cambios con observaciones (10 a 5000 caracteres)
ALTER TABLE public.revisiones
    DROP CONSTRAINT IF EXISTS ck_revisiones_docente;

ALTER TABLE public.revisiones
    ADD CONSTRAINT ck_revisiones_docente
    CHECK (
        ambito_revision <> 'Revisión docente'
        OR fecha_cierre IS NULL
        OR (
            resultado IN ('Conforme', 'Requiere cambios')
            AND motivo_decision IS NOT NULL
            AND char_length(btrim(motivo_decision)) BETWEEN 10 AND 5000
        )
    );

CREATE INDEX IF NOT EXISTS revisiones_docente_ultima
    ON public.revisiones (id_entrega, fecha_cierre DESC, id_revision DESC)
    WHERE ambito_revision = 'Revisión docente' AND fecha_cierre IS NOT NULL;
