\set ON_ERROR_STOP on

DO $$
DECLARE
    owner_oid oid;
    app_oid oid;
    config_table_name text;
    config_table_oid regclass;
    migration_history_oid regclass;
    actual_output_fields text[];
    actual_europe_scope text[];
BEGIN
    SELECT oid
    INTO owner_oid
    FROM pg_roles
    WHERE rolname = 'pdg_platform_owner';

    IF owner_oid IS NULL THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_owner does not exist';
    END IF;

    SELECT oid
    INTO app_oid
    FROM pg_roles
    WHERE rolname = 'pdg_platform_app';

    IF app_oid IS NULL THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app does not exist';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_roles
        WHERE oid = app_oid
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
            'Platform security verification failed: unsafe pdg_platform_app role attributes';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_auth_members
        WHERE member = app_oid
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app has role memberships';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_namespace
        WHERE nspname = 'pdg'
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg schema does not exist';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_namespace
        WHERE nspname = 'pdg'
          AND nspowner <> owner_oid
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg schema owner is not pdg_platform_owner';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pg_class AS c
        JOIN pg_namespace AS n
          ON n.oid = c.relnamespace
        WHERE n.nspname = 'pdg'
          AND c.relowner <> owner_oid
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: an object in pdg is not owned by pdg_platform_owner';
    END IF;

    IF NOT has_database_privilege(
        'pdg_platform_app',
        current_database(),
        'CONNECT')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app lacks CONNECT';
    END IF;

    IF has_database_privilege(
        'pdg_platform_app',
        current_database(),
        'CREATE')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app has database CREATE privilege';
    END IF;

    IF NOT has_schema_privilege(
        'pdg_platform_app',
        'pdg',
        'USAGE')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app lacks pdg schema USAGE';
    END IF;

    IF has_schema_privilege(
        'pdg_platform_app',
        'pdg',
        'CREATE')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app has schema CREATE privilege';
    END IF;

    FOREACH config_table_name IN ARRAY ARRAY[
        'subject',
        'actor',
        'actor_capability',
        'delegation',
        'resource',
        'resource_parameter',
        'resource_output_field',
        'subject_resource_permission',
        'subject_row_scope'
    ]
    LOOP
        config_table_oid :=
            to_regclass(
                format(
                    '%I.%I',
                    'pdg',
                    config_table_name));

        IF config_table_oid IS NULL THEN
            RAISE EXCEPTION
                'Platform security verification failed: configuration table % does not exist',
                config_table_name;
        END IF;

        IF NOT has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'SELECT')
        THEN
            RAISE EXCEPTION
                'Platform security verification failed: pdg_platform_app lacks SELECT on %',
                config_table_name;
        END IF;

        IF has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'INSERT')
           OR has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'UPDATE')
           OR has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'DELETE')
           OR has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'TRUNCATE')
           OR has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'REFERENCES')
           OR has_table_privilege(
            'pdg_platform_app',
            config_table_oid,
            'TRIGGER')
        THEN
            RAISE EXCEPTION
                'Platform security verification failed: pdg_platform_app has write privilege on configuration table %',
                config_table_name;
        END IF;
    END LOOP;

    IF to_regclass('pdg.audit_record') IS NULL THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg.audit_record does not exist';
    END IF;

    IF NOT has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'SELECT')
       OR NOT has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'INSERT')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app lacks required audit privileges';
    END IF;

    IF has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'UPDATE')
       OR has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'DELETE')
       OR has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'TRUNCATE')
       OR has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'REFERENCES')
       OR has_table_privilege(
        'pdg_platform_app',
        'pdg.audit_record',
        'TRIGGER')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app has forbidden audit write privileges';
    END IF;

    IF to_regclass('pdg.audit_record_audit_id_seq') IS NULL THEN
        RAISE EXCEPTION
            'Platform security verification failed: audit identity sequence does not exist';
    END IF;

    IF NOT has_sequence_privilege(
        'pdg_platform_app',
        'pdg.audit_record_audit_id_seq',
        'USAGE')
       OR NOT has_sequence_privilege(
        'pdg_platform_app',
        'pdg.audit_record_audit_id_seq',
        'SELECT')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app lacks required audit sequence privileges';
    END IF;

    IF has_sequence_privilege(
        'pdg_platform_app',
        'pdg.audit_record_audit_id_seq',
        'UPDATE')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app has forbidden audit sequence UPDATE privilege';
    END IF;

    migration_history_oid :=
        to_regclass('"pdg"."__EFMigrationsHistory"');

    IF migration_history_oid IS NULL THEN
        RAISE EXCEPTION
            'Platform security verification failed: migration history table does not exist';
    END IF;

    IF has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'SELECT')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'INSERT')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'UPDATE')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'DELETE')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'TRUNCATE')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'REFERENCES')
       OR has_table_privilege(
        'pdg_platform_app',
        migration_history_oid,
        'TRIGGER')
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: pdg_platform_app can access migration history';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pdg.subject
        WHERE subject_id = 'user_42'
          AND role_code = 'SalesManagerEurope'
    )
       OR NOT EXISTS (
        SELECT 1
        FROM pdg.subject
        WHERE subject_id = 'user_43'
          AND role_code = 'GlobalAnalyst'
    )
       OR NOT EXISTS (
        SELECT 1
        FROM pdg.subject
        WHERE subject_id = 'user_44'
          AND role_code = 'SupportAgent'
    )
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: mandatory demo subjects are missing or invalid';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pdg.actor
        WHERE actor_id = 'sales_copilot_v1'
          AND actor_type = 'ai_assistant'
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: mandatory demo actor is missing or invalid';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pdg.actor_capability
        WHERE actor_id = 'sales_copilot_v1'
          AND capability = 'sales.read'
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: mandatory actor capability is missing';
    END IF;

    IF (
        SELECT count(*)
        FROM pdg.delegation
        WHERE actor_id = 'sales_copilot_v1'
          AND subject_id IN (
              'user_42',
              'user_43',
              'user_44'
          )
          AND is_active
    ) <> 3
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: mandatory delegations are missing or inactive';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pdg.resource
        WHERE resource_name = 'SalesSummary'
          AND required_capability = 'sales.read'
          AND max_rows = 500
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: SalesSummary resource configuration is invalid';
    END IF;

    IF (
        SELECT count(*)
        FROM pdg.resource_parameter
        WHERE resource_name = 'SalesSummary'
    ) <> 1
       OR NOT EXISTS (
        SELECT 1
        FROM pdg.resource_parameter
        WHERE resource_name = 'SalesSummary'
          AND param_name = 'country'
          AND param_type = 'string'
          AND required = FALSE
    )
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: SalesSummary parameter contract is invalid';
    END IF;

    SELECT array_agg(
        ordinal::text || ':' || field_name
        ORDER BY ordinal)
    INTO actual_output_fields
    FROM pdg.resource_output_field
    WHERE resource_name = 'SalesSummary';

    IF actual_output_fields IS DISTINCT FROM
        ARRAY[
            '1:CustomerId',
            '2:Country',
            '3:InvoiceDate',
            '4:Total'
        ]::text[]
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: SalesSummary output contract is invalid';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pdg.subject_resource_permission
        WHERE subject_id = 'user_42'
          AND resource_name = 'SalesSummary'
          AND allowed = TRUE
          AND row_scope_mode = 'ALLOW_LIST'
    )
       OR NOT EXISTS (
        SELECT 1
        FROM pdg.subject_resource_permission
        WHERE subject_id = 'user_43'
          AND resource_name = 'SalesSummary'
          AND allowed = TRUE
          AND row_scope_mode = 'ALL'
    )
       OR NOT EXISTS (
        SELECT 1
        FROM pdg.subject_resource_permission
        WHERE subject_id = 'user_44'
          AND resource_name = 'SalesSummary'
          AND allowed = FALSE
          AND row_scope_mode = 'ALL'
    )
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: mandatory resource permissions are invalid';
    END IF;

    SELECT array_agg(
        allowed_value
        ORDER BY allowed_value)
    INTO actual_europe_scope
    FROM pdg.subject_row_scope
    WHERE subject_id = 'user_42'
      AND resource_name = 'SalesSummary'
      AND dimension = 'country';

    IF actual_europe_scope IS DISTINCT FROM
        ARRAY[
            'Austria',
            'Belgium',
            'Czech Republic',
            'Denmark',
            'Finland',
            'France',
            'Germany',
            'Hungary',
            'Ireland',
            'Italy',
            'Netherlands',
            'Norway',
            'Poland',
            'Portugal',
            'Spain',
            'Sweden',
            'United Kingdom'
        ]::text[]
    THEN
        RAISE EXCEPTION
            'Platform security verification failed: Europe row scope is invalid';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM pdg.subject_row_scope
        WHERE subject_id IN ('user_43', 'user_44')
          AND resource_name = 'SalesSummary'
    ) THEN
        RAISE EXCEPTION
            'Platform security verification failed: unexpected row scope exists for ALL/DENY subjects';
    END IF;

    RAISE NOTICE
        'Platform security verification passed';
END
$$;