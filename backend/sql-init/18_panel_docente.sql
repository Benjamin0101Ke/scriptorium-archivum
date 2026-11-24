BEGIN;
CREATE TABLE IF NOT EXISTS public.revisiones_docentes (
    id_revision bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_trabajo integer NOT NULL REFERENCES public.trabajos(id_trabajo),
    id_entrega integer NOT NULL,
    id_docente integer NOT NULL REFERENCES public.usuarios(id_usuario),
    resultado varchar(30) NOT NULL CHECK (resultado IN ('Conforme','Requiere cambios')),
    observaciones text NOT NULL CHECK (length(btrim(observaciones)) BETWEEN 10 AND 5000),
    creado_en timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (id_trabajo,id_entrega) REFERENCES public.entregas(id_trabajo,id_entrega)
);
CREATE INDEX IF NOT EXISTS revisiones_docentes_trabajo ON public.revisiones_docentes(id_trabajo,id_revision DESC);
COMMIT;
