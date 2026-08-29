\set ON_ERROR_STOP on

DO $$
DECLARE
    reader_oid oid;
    actual_columns text[];
BEGIN
    SELECT oid
    INTO reader_oid
    FROM pg_roles
    WHERE rolname = 'pdg_reader';

    IF reader_oid IS NULL THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader does not exist';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_roles
        WHERE oid = reader_oid
          AND (
              rolsuper
              OR rolcreatedb
              OR rolcreaterole
              OR rolreplication
              OR rolbypassrls
              OR rolinherit
              OR NOT rolcanlogin
          )
    ) THEN
        RAISE EXCEPTION
            'Corporate security verification failed: unsafe pdg_reader role attributes';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_auth_members
        WHERE member = reader_oid
    ) THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader has role memberships';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class AS c
        JOIN pg_namespace AS n
          ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relname IN ('customer', 'invoice', 'invoice_line')
          AND c.relowner = reader_oid
    ) THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader owns protected base tables';
    END IF;

    IF has_database_privilege(
        'pdg_reader',
        'chinook',
        'CREATE')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader has database CREATE privilege';
    END IF;

    IF has_schema_privilege(
        'pdg_reader',
        'public',
        'CREATE')
       OR has_schema_privilege(
        'pdg_reader',
        'pdg',
        'CREATE')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader has schema CREATE privilege';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class AS c
        JOIN pg_namespace AS n
          ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind IN ('r', 'p', 'v', 'm', 'f')
          AND (
              has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'SELECT')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'INSERT')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'UPDATE')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'DELETE')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'TRUNCATE')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'REFERENCES')
              OR has_table_privilege(
                  'pdg_reader',
                  c.oid,
                  'TRIGGER')
          )
    ) THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader can access public data objects';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class AS c
        JOIN pg_namespace AS n
          ON n.oid = c.relnamespace
        WHERE n.nspname = 'public'
          AND c.relkind = 'S'
          AND (
              has_sequence_privilege(
                  'pdg_reader',
                  c.oid,
                  'USAGE')
              OR has_sequence_privilege(
                  'pdg_reader',
                  c.oid,
                  'SELECT')
              OR has_sequence_privilege(
                  'pdg_reader',
                  c.oid,
                  'UPDATE')
          )
    ) THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader has public sequence privileges';
    END IF;

    IF to_regclass('pdg.sales_summary') IS NULL THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg.sales_summary does not exist';
    END IF;

    SELECT array_agg(
        a.attname::text
        ORDER BY a.attnum)
    INTO actual_columns
    FROM pg_attribute AS a
    WHERE a.attrelid =
              'pdg.sales_summary'::regclass
      AND a.attnum > 0
      AND NOT a.attisdropped;

    IF actual_columns IS DISTINCT FROM
        ARRAY[
            'CustomerId',
            'Country',
            'InvoiceDate',
            'Total'
        ]::text[]
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: unexpected sales_summary projection';
    END IF;

    IF NOT has_database_privilege(
        'pdg_reader',
        'chinook',
        'CONNECT')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader lacks CONNECT';
    END IF;

    IF NOT has_schema_privilege(
        'pdg_reader',
        'pdg',
        'USAGE')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader lacks pdg schema USAGE';
    END IF;

    IF NOT has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'SELECT')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader lacks sales_summary SELECT';
    END IF;

    IF has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'INSERT')
       OR has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'UPDATE')
       OR has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'DELETE')
       OR has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'TRUNCATE')
       OR has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'REFERENCES')
       OR has_table_privilege(
        'pdg_reader',
        'pdg.sales_summary',
        'TRIGGER')
    THEN
        RAISE EXCEPTION
            'Corporate security verification failed: pdg_reader has write privileges on sales_summary';
    END IF;

    RAISE NOTICE
        'Corporate security verification passed';
END
$$;