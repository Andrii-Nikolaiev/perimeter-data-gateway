\set ON_ERROR_STOP on

SELECT 'CREATE ROLE pdg_reader'
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_roles
    WHERE rolname = 'pdg_reader'
)
\gexec

ALTER ROLE pdg_reader
    WITH LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION
    NOBYPASSRLS
    NOINHERIT
    PASSWORD :'pdg_reader_password';