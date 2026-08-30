using System.Text.Json;
using Npgsql;
using Perimeter.Gateway.AcceptanceTests.Fixtures;

namespace Perimeter.Gateway.AcceptanceTests.Helpers;

public sealed class AuditAssertions
{
    private readonly AcceptanceEnvironment _environment;

    public AuditAssertions(
        AcceptanceEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<long> GetLatestAuditIdAsync(
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
                """
                SELECT COALESCE(MAX(audit_id), 0)
                FROM pdg.audit_record;
                """,
                connection);

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct));
    }

    public async Task<AuditSnapshot> GetLatestAfterAsync(
        long afterAuditId,
        string subjectId,
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
                """
                SELECT
                    audit_id,
                    occurred_at,
                    subject_id,
                    actor_id,
                    capability,
                    resource_name,
                    scope,
                    decision,
                    reason_category,
                    normalized_parameters::text,
                    effective_row_scope::text,
                    rows_returned
                FROM pdg.audit_record
                WHERE audit_id > @after_audit_id
                  AND subject_id = @subject_id
                ORDER BY audit_id DESC
                LIMIT 1;
                """,
                connection);

        command.Parameters.AddWithValue(
            "after_audit_id",
            afterAuditId);

        command.Parameters.AddWithValue(
            "subject_id",
            subjectId);

        await using var reader =
            await command.ExecuteReaderAsync(ct);

        Assert.True(
            await reader.ReadAsync(ct),
            "Expected a new audit record, but none was written.");

        return new AuditSnapshot(
            reader.GetInt64(0),
            reader.GetFieldValue<DateTimeOffset>(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetString(9),
            reader.IsDBNull(10)
                ? null
                : reader.GetString(10),
            reader.GetInt32(11));
    }

    public static void AssertRequiredFields(
        AuditSnapshot record,
        bool requireScope = true)
    {
        Assert.True(record.AuditId > 0);

        Assert.NotEqual(
            default,
            record.OccurredAt);

        Assert.False(
            string.IsNullOrWhiteSpace(record.SubjectId));

        Assert.False(
            string.IsNullOrWhiteSpace(record.ActorId));

        Assert.False(
            string.IsNullOrWhiteSpace(record.Capability));

        Assert.False(
            string.IsNullOrWhiteSpace(record.ResourceName));

        if (requireScope)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(record.Scope));
        }

        Assert.Contains(
            record.Decision,
            new[] { "ALLOW", "DENY" });

        Assert.False(
            string.IsNullOrWhiteSpace(
                record.ReasonCategory));

        Assert.True(record.RowsReturned >= 0);

        using var normalizedParameters =
            JsonDocument.Parse(
                record.NormalizedParametersJson);

        Assert.Equal(
            JsonValueKind.Object,
            normalizedParameters
                .RootElement
                .ValueKind);

        if (record.EffectiveRowScopeJson is not null)
        {
            using var effectiveRowScope =
                JsonDocument.Parse(
                    record.EffectiveRowScopeJson);

            Assert.Equal(
                JsonValueKind.Object,
                effectiveRowScope
                    .RootElement
                    .ValueKind);
        }

        if (record.Decision == "DENY")
        {
            Assert.Equal(
                0,
                record.RowsReturned);
        }
    }

    public static void AssertNoSensitivePayload(
        AuditSnapshot record)
    {
        var storedContent =
            string.Join(
                "\n",
                record.SubjectId,
                record.ActorId,
                record.Capability,
                record.ResourceName,
                record.Scope,
                record.Decision,
                record.ReasonCategory,
                record.NormalizedParametersJson,
                record.EffectiveRowScopeJson
                    ?? string.Empty);

        var forbiddenFragments =
            new[]
            {
                "Bearer ",
                "signing_key",
                "signing key",
                "password",
                "connectionstring",
                "connection string",
                "raw_prompt",
                "raw prompt",
                "full_response",
                "full response"
            };

        foreach (var forbiddenFragment in forbiddenFragments)
        {
            Assert.DoesNotContain(
                forbiddenFragment,
                storedContent,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed record AuditSnapshot(
        long AuditId,
        DateTimeOffset OccurredAt,
        string SubjectId,
        string ActorId,
        string Capability,
        string ResourceName,
        string Scope,
        string Decision,
        string ReasonCategory,
        string NormalizedParametersJson,
        string? EffectiveRowScopeJson,
        int RowsReturned);
}