\set ON_ERROR_STOP on

SELECT 'CREATE ROLE pdg_platform_app'
WHERE NOT EXISTS (
    SELECT 1
    FROM pg_roles
    WHERE rolname = 'pdg_platform_app'
)
\gexec

ALTER ROLE pdg_platform_app
    WITH LOGIN
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION
    NOBYPASSRLS
    NOINHERIT
    PASSWORD :'platform_app_password';