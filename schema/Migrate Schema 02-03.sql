DROP TABLE IF EXISTS public.next_connection;

CREATE TABLE public.next_connection (
	db varchar NULL,
	ldap varchar NULL
);

COMMENT ON COLUMN public.next_connection.db IS 'The database connection string.';
COMMENT ON COLUMN public.next_connection.ldap IS 'The LDAP server.';

INSERT INTO public.next_connection (db, ldap) VALUES('Server=localhost;Port=5432;User Id=user;Password="password";Database=mappings;Pooling=false', 'ldat.site.de');

UPDATE public.schemaversion
	SET schemaversion=3
	WHERE schemaversion=2;
