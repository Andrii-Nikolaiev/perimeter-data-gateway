
INSERT INTO pdg.subject (
    subject_id,
    role_code
)
VALUES
    ('user_42', 'SalesManagerEurope'),
    ('user_43', 'GlobalAnalyst'),
    ('user_44', 'SupportAgent')
ON CONFLICT (subject_id) DO UPDATE
SET role_code = EXCLUDED.role_code;

INSERT INTO pdg.actor (
    actor_id,
    actor_type
)
VALUES (
    'sales_copilot_v1',
    'ai_assistant'
)
ON CONFLICT (actor_id) DO UPDATE
SET actor_type = EXCLUDED.actor_type;

INSERT INTO pdg.actor_capability (
    actor_id,
    capability
)
VALUES (
    'sales_copilot_v1',
    'sales.read'
)
ON CONFLICT (actor_id, capability) DO NOTHING;

INSERT INTO pdg.delegation (
    subject_id,
    actor_id,
    is_active
)
VALUES
    ('user_42', 'sales_copilot_v1', TRUE),
    ('user_43', 'sales_copilot_v1', TRUE),
    ('user_44', 'sales_copilot_v1', TRUE)
ON CONFLICT (subject_id, actor_id) DO UPDATE
SET is_active = EXCLUDED.is_active;

INSERT INTO pdg.resource (
    resource_name,
    required_capability,
    max_rows
)
VALUES (
    'SalesSummary',
    'sales.read',
    500
)
ON CONFLICT (resource_name) DO UPDATE
SET required_capability = EXCLUDED.required_capability,
    max_rows = EXCLUDED.max_rows;

INSERT INTO pdg.resource_parameter (
    resource_name,
    param_name,
    param_type,
    required
)
VALUES (
    'SalesSummary',
    'country',
    'string',
    FALSE
)
ON CONFLICT (resource_name, param_name) DO UPDATE
SET param_type = EXCLUDED.param_type,
    required = EXCLUDED.required;

INSERT INTO pdg.resource_output_field (
    resource_name,
    field_name,
    ordinal
)
VALUES
    ('SalesSummary', 'CustomerId', 1),
    ('SalesSummary', 'Country', 2),
    ('SalesSummary', 'InvoiceDate', 3),
    ('SalesSummary', 'Total', 4)
ON CONFLICT (resource_name, field_name) DO UPDATE
SET ordinal = EXCLUDED.ordinal;

INSERT INTO pdg.subject_resource_permission (
    subject_id,
    resource_name,
    allowed,
    row_scope_mode
)
VALUES
    ('user_42', 'SalesSummary', TRUE, 'ALLOW_LIST'),
    ('user_43', 'SalesSummary', TRUE, 'ALL'),
    ('user_44', 'SalesSummary', FALSE, 'ALL')
ON CONFLICT (subject_id, resource_name) DO UPDATE
SET allowed = EXCLUDED.allowed,
    row_scope_mode = EXCLUDED.row_scope_mode;

INSERT INTO pdg.subject_row_scope (
    subject_id,
    resource_name,
    dimension,
    allowed_value
)
VALUES
    ('user_42', 'SalesSummary', 'country', 'Austria'),
    ('user_42', 'SalesSummary', 'country', 'Belgium'),
    ('user_42', 'SalesSummary', 'country', 'Czech Republic'),
    ('user_42', 'SalesSummary', 'country', 'Denmark'),
    ('user_42', 'SalesSummary', 'country', 'Finland'),
    ('user_42', 'SalesSummary', 'country', 'France'),
    ('user_42', 'SalesSummary', 'country', 'Germany'),
    ('user_42', 'SalesSummary', 'country', 'Hungary'),
    ('user_42', 'SalesSummary', 'country', 'Ireland'),
    ('user_42', 'SalesSummary', 'country', 'Italy'),
    ('user_42', 'SalesSummary', 'country', 'Netherlands'),
    ('user_42', 'SalesSummary', 'country', 'Norway'),
    ('user_42', 'SalesSummary', 'country', 'Poland'),
    ('user_42', 'SalesSummary', 'country', 'Portugal'),
    ('user_42', 'SalesSummary', 'country', 'Spain'),
    ('user_42', 'SalesSummary', 'country', 'Sweden'),
    ('user_42', 'SalesSummary', 'country', 'United Kingdom')
ON CONFLICT (
    subject_id,
    resource_name,
    dimension,
    allowed_value
) DO NOTHING;
