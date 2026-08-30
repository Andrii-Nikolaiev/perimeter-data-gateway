using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.Helpers;

public sealed class AcceptanceDatabaseMutator
{
    private const string SubjectId = "user_42";
    private const string ActorId = "sales_copilot_v1";
    private const string ResourceName = "SalesSummary";
    private const string Capability = "sales.read";
    private const string RowScopeDimension = "country";
    private const string SyntheticInvoiceMarker =
        "PDG_ACCEPTANCE_T17";

    private static readonly string[] EuropeCountries =
    {
        "Austria",
        "Belgium",
        "Czech Republic",
        "Denmark",
        "Finland",
        "France",
        "Germany",
        "Hungary",
        "Ireland",
        "Italy",
        "Netherlands",
        "Norway",
        "Poland",
        "Portugal",
        "Spain",
        "Sweden",
        "United Kingdom"
    };

    private readonly AcceptanceEnvironment _environment;

    public AcceptanceDatabaseMutator(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    public Task DisableDelegationAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            UPDATE pdg.delegation
            SET is_active = FALSE
            WHERE subject_id = @subject_id
              AND actor_id = @actor_id;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "subject_id",
                    SubjectId);

                command.Parameters.AddWithValue(
                    "actor_id",
                    ActorId);
            },
            ct);
    }

    public Task RestoreDelegationAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            INSERT INTO pdg.delegation (
                subject_id,
                actor_id,
                is_active
            )
            VALUES (
                @subject_id,
                @actor_id,
                TRUE
            )
            ON CONFLICT (subject_id, actor_id) DO UPDATE
            SET is_active = TRUE;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "subject_id",
                    SubjectId);

                command.Parameters.AddWithValue(
                    "actor_id",
                    ActorId);
            },
            ct);
    }

    public Task RemoveActorCapabilityAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            DELETE FROM pdg.actor_capability
            WHERE actor_id = @actor_id
              AND capability = @capability;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "actor_id",
                    ActorId);

                command.Parameters.AddWithValue(
                    "capability",
                    Capability);
            },
            ct);
    }

    public Task RestoreActorCapabilityAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            INSERT INTO pdg.actor_capability (
                actor_id,
                capability
            )
            VALUES (
                @actor_id,
                @capability
            )
            ON CONFLICT (actor_id, capability) DO NOTHING;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "actor_id",
                    ActorId);

                command.Parameters.AddWithValue(
                    "capability",
                    Capability);
            },
            ct);
    }

    public Task RemoveSubjectRowScopeAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            DELETE FROM pdg.subject_row_scope
            WHERE subject_id = @subject_id
              AND resource_name = @resource_name
              AND dimension = @dimension;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "subject_id",
                    SubjectId);

                command.Parameters.AddWithValue(
                    "resource_name",
                    ResourceName);

                command.Parameters.AddWithValue(
                    "dimension",
                    RowScopeDimension);
            },
            ct);
    }

    public Task RestoreSubjectRowScopeAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            INSERT INTO pdg.subject_row_scope (
                subject_id,
                resource_name,
                dimension,
                allowed_value
            )
            SELECT
                @subject_id,
                @resource_name,
                @dimension,
                allowed_value
            FROM unnest(@allowed_values::text[])
                AS allowed_value
            ON CONFLICT (
                subject_id,
                resource_name,
                dimension,
                allowed_value
            ) DO NOTHING;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "subject_id",
                    SubjectId);

                command.Parameters.AddWithValue(
                    "resource_name",
                    ResourceName);

                command.Parameters.AddWithValue(
                    "dimension",
                    RowScopeDimension);

                command.Parameters.AddWithValue(
                    "allowed_values",
                    EuropeCountries);
            },
            ct);
    }

    public Task RevokeAuditInsertAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            REVOKE INSERT
            ON TABLE pdg.audit_record
            FROM pdg_platform_app;
            """,
            configure: null,
            ct);
    }

    public Task RestoreAuditInsertAsync(
        CancellationToken ct)
    {
        return ExecutePlatformAsync(
            """
            GRANT INSERT
            ON TABLE pdg.audit_record
            TO pdg_platform_app;
            """,
            configure: null,
            ct);
    }

    public async Task<int> AddSyntheticInvoicesBeyondLimitAsync(
        int limit,
        CancellationToken ct)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit));
        }

        await RemoveSyntheticInvoicesAsync(ct);

        await using var connection =
            new NpgsqlConnection(
                _environment
                    .CorporateData
                    .OwnerConnectionString);

        await connection.OpenAsync(ct);

        int currentRows;

        await using (var countCommand =
            new NpgsqlCommand(
                """
                SELECT count(*)
                FROM pdg.sales_summary;
                """,
                connection))
        {
            currentRows =
                Convert.ToInt32(
                    await countCommand.ExecuteScalarAsync(ct));
        }

        var rowsToAdd =
            Math.Max(
                1,
                checked(limit + 1 - currentRows));

        await using var insertCommand =
            new NpgsqlCommand(
                """
                WITH source_invoice AS (
                    SELECT
                        customer_id,
                        invoice_date,
                        billing_city,
                        billing_state,
                        billing_country,
                        billing_postal_code,
                        total
                    FROM public.invoice
                    ORDER BY invoice_id
                    LIMIT 1
                ),
                current_max AS (
                    SELECT
                        COALESCE(MAX(invoice_id), 0) AS max_id
                    FROM public.invoice
                )
                INSERT INTO public.invoice (
                    invoice_id,
                    customer_id,
                    invoice_date,
                    billing_address,
                    billing_city,
                    billing_state,
                    billing_country,
                    billing_postal_code,
                    total
                )
                SELECT
                    current_max.max_id + generated.n,
                    source_invoice.customer_id,
                    source_invoice.invoice_date,
                    @marker,
                    source_invoice.billing_city,
                    source_invoice.billing_state,
                    source_invoice.billing_country,
                    source_invoice.billing_postal_code,
                    source_invoice.total
                FROM source_invoice
                CROSS JOIN current_max
                CROSS JOIN generate_series(
                    1,
                    @rows_to_add
                ) AS generated(n);
                """,
                connection);

        insertCommand.Parameters.AddWithValue(
            "marker",
            SyntheticInvoiceMarker);

        insertCommand.Parameters.AddWithValue(
            "rows_to_add",
            rowsToAdd);

        var inserted =
            await insertCommand.ExecuteNonQueryAsync(ct);

        if (inserted != rowsToAdd)
        {
            throw new InvalidOperationException(
                "Synthetic invoice fixture did not create " +
                $"the expected row count. Expected={rowsToAdd}, " +
                $"Inserted={inserted}.");
        }

        return inserted;
    }

    public Task RemoveSyntheticInvoicesAsync(
        CancellationToken ct)
    {
        return ExecuteCorporateAsync(
            """
            DELETE FROM public.invoice
            WHERE billing_address = @marker;
            """,
            command =>
            {
                command.Parameters.AddWithValue(
                    "marker",
                    SyntheticInvoiceMarker);
            },
            ct);
    }

    private async Task ExecutePlatformAsync(
        string sql,
        Action<NpgsqlCommand>? configure,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _environment
                    .PlatformStore
                    .OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                sql,
                connection);

        configure?.Invoke(command);

        await command.ExecuteNonQueryAsync(ct);
    }

    private async Task ExecuteCorporateAsync(
        string sql,
        Action<NpgsqlCommand>? configure,
        CancellationToken ct)
    {
        await using var connection =
            new NpgsqlConnection(
                _environment
                    .CorporateData
                    .OwnerConnectionString);

        await connection.OpenAsync(ct);

        await using var command =
            new NpgsqlCommand(
                sql,
                connection);

        configure?.Invoke(command);

        await command.ExecuteNonQueryAsync(ct);
    }
}
