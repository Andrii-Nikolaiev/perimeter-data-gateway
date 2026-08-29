\set ON_ERROR_STOP on

REVOKE ALL PRIVILEGES
    ON DATABASE chinook
    FROM pdg_reader;

REVOKE ALL PRIVILEGES
    ON SCHEMA public
    FROM pdg_reader;

REVOKE ALL PRIVILEGES
    ON SCHEMA pdg
    FROM pdg_reader;

REVOKE ALL PRIVILEGES
    ON ALL TABLES IN SCHEMA public
    FROM pdg_reader;

REVOKE ALL PRIVILEGES
    ON ALL SEQUENCES IN SCHEMA public
    FROM pdg_reader;

REVOKE ALL PRIVILEGES
    ON ALL TABLES IN SCHEMA pdg
    FROM pdg_reader;

GRANT CONNECT
    ON DATABASE chinook
    TO pdg_reader;

GRANT USAGE
    ON SCHEMA pdg
    TO pdg_reader;

GRANT SELECT
    ON pdg.sales_summary
    TO pdg_reader;