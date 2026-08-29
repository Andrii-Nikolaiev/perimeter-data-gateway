\set ON_ERROR_STOP on

REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA pdg
FROM pdg_platform_app;

REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA pdg
FROM pdg_platform_app;

REVOKE ALL ON SCHEMA pdg
FROM pdg_platform_app;

GRANT USAGE ON SCHEMA pdg
TO pdg_platform_app;

GRANT SELECT ON
    pdg.subject,
    pdg.actor,
    pdg.actor_capability,
    pdg.delegation,
    pdg.resource,
    pdg.resource_parameter,
    pdg.resource_output_field,
    pdg.subject_resource_permission,
    pdg.subject_row_scope
TO pdg_platform_app;

GRANT SELECT, INSERT ON
    pdg.audit_record
TO pdg_platform_app;

GRANT USAGE, SELECT ON SEQUENCE
    pdg.audit_record_audit_id_seq
TO pdg_platform_app;