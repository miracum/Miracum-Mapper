ALTER TABLE public.next_connection ADD visualizer varchar NULL;

UPDATE public.schemaversion	SET schemaversion=4	WHERE schemaversion=3;
