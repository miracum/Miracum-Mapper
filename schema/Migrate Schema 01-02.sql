DROP TABLE public.schemaversion;

CREATE TABLE public.schemaversion (
	schemaversion int4 NULL -- The current schemaversion.
);
COMMENT ON TABLE public.schemaversion IS 'Tells the version number of the DB schema for DB schema versions greater 1.';

-- Column comments

COMMENT ON COLUMN public.schemaversion.schemaversion IS 'The current DB schema version.';

INSERT INTO public.schemaversion (schemaversion) VALUES(2);

ALTER TABLE public.userids ADD local_password_md5 varchar NULL;
COMMENT ON COLUMN public.userids.local_password_md5 IS 'Local password MD5 hash. If this is set, LDAP authentication is not used. Introduced with version 1.8.0.';
