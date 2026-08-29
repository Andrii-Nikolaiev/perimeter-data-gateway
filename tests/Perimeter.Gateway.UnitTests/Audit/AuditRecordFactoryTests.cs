using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Audit;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.UnitTests.Audit;

public sealed class AuditRecordFactoryTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 8, 29, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_AllowDecision_BuildsExpectedAuditRecord()
    {
        var factory = new AuditRecordFactory(
            new FixedClock(FixedTime));

        var token = CreateToken(
            "sales.read",
            "profile.read");

        var effectiveScope = new RowScope(
            RowScopeMode.AllowList,
            new Dictionary<string, IReadOnlySet<string>>
            {
                ["country"] = new HashSet<string>(
                    new[] { "Germany" },
                    StringComparer.Ordinal)
            });

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "sales.read",
            "SalesSummary",
            effectiveScope,
            "authorized");

        var parameters =
            new Dictionary<string, string?>
            {
                ["country"] = "Germany"
            };

        var record = factory.Create(
            token,
            decision,
            parameters,
            "authorized",
            12);

        Assert.Equal(FixedTime, record.Timestamp);
        Assert.Equal("user_42", record.Subject);
        Assert.Equal("sales_copilot_v1", record.Actor);
        Assert.Equal("sales.read", record.Capability);
        Assert.Equal("SalesSummary", record.Resource);
        Assert.Equal("profile.read sales.read", record.Scope);
        Assert.Equal("ALLOW", record.Decision);
        Assert.Equal("authorized", record.ReasonCategory);
        Assert.Equal("Germany", record.NormalizedParameters["country"]);
        Assert.Same(effectiveScope, record.EffectiveRowScope);
        Assert.Equal(12, record.RowsReturned);
    }

    [Fact]
    public void Create_DenyDecision_BuildsZeroRowAuditRecord()
    {
        var factory = new AuditRecordFactory(
            new FixedClock(FixedTime));

        var token = CreateToken("sales.read");

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Deny,
            "sales.read",
            "SalesSummary",
            null,
            "row_scope_not_allowed");

        var record = factory.Create(
            token,
            decision,
            new Dictionary<string, string?>
            {
                ["country"] = "USA"
            },
            "row_scope_not_allowed",
            0);

        Assert.Equal("DENY", record.Decision);
        Assert.Equal(
            "row_scope_not_allowed",
            record.ReasonCategory);
        Assert.Null(record.EffectiveRowScope);
        Assert.Equal(0, record.RowsReturned);
    }

    [Fact]
    public void Create_ScopesAreWrittenInDeterministicOrder()
    {
        var factory = new AuditRecordFactory(
            new FixedClock(FixedTime));

        var token = CreateToken(
            "z.scope",
            "a.scope",
            "m.scope");

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "a.scope",
            "SalesSummary",
            new RowScope(
                RowScopeMode.All,
                new Dictionary<string, IReadOnlySet<string>>()),
            "authorized");

        var record = factory.Create(
            token,
            decision,
            new Dictionary<string, string?>(),
            "authorized",
            0);

        Assert.Equal(
            "a.scope m.scope z.scope",
            record.Scope);
    }

    [Fact]
    public void Create_DenyWithRowsReturned_ThrowsInternalError()
    {
        var factory = new AuditRecordFactory(
            new FixedClock(FixedTime));

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Deny,
            "sales.read",
            "SalesSummary",
            null,
            "subject_resource_not_allowed");

        var exception = Assert.Throws<PdgException>(
            () => factory.Create(
                CreateToken("sales.read"),
                decision,
                new Dictionary<string, string?>(),
                "subject_resource_not_allowed",
                1));

        Assert.Equal(
            PdgErrorCategory.InternalError,
            exception.Category);
    }

    private static ValidatedTokenContext CreateToken(
        params string[] scopes)
    {
        return new ValidatedTokenContext(
            "user_42",
            "sales_copilot_v1",
            new HashSet<string>(
                scopes,
                StringComparer.Ordinal));
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}