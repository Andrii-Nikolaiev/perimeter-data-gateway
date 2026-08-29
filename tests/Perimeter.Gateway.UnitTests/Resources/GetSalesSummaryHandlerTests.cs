using Moq;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Audit;
using Perimeter.Gateway.Application.Authorization;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Application.Resources;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.UnitTests.Resources;

public sealed class GetSalesSummaryHandlerTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 8, 29, 8, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_Allow_WritesAuditAndReturnsBufferedRows()
    {
        var evaluator = new Mock<IAccessPolicyEvaluator>();
        var platformStore = new Mock<IPlatformStore>();
        var corporateReader = new Mock<ICorporateDataReader>();
        var auditWriter = new Mock<IAuditWriter>();

        var resource = CreateResource();
        var effectiveScope = CreateCountryScope("Germany");

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "sales.read",
            "SalesSummary",
            effectiveScope,
            AuthorizationReasonCategories.Authorized);

        var rows = (IReadOnlyList<SalesSummaryRow>)new[]
        {
            new SalesSummaryRow(
                2,
                "Germany",
                new DateOnly(2021, 1, 1),
                1.98m),
            new SalesSummaryRow(
                3,
                "Germany",
                new DateOnly(2021, 1, 2),
                3.96m)
        };

        platformStore
            .Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        evaluator
            .Setup(x => x.EvaluateAsync(
                It.IsAny<ValidatedTokenContext>(),
                "SalesSummary",
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        corporateReader
            .Setup(x => x.ReadSalesSummaryAsync(
                effectiveScope,
                501,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        AuditRecord? writtenAudit = null;

        auditWriter
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>(
                (record, _) => writtenAudit = record)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            evaluator,
            platformStore,
            corporateReader,
            auditWriter);

        var result = await handler.HandleAsync(
            CreateRequest("Germany"),
            CancellationToken.None);

        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(500, result.Limit);

        Assert.NotNull(writtenAudit);
        Assert.Equal("ALLOW", writtenAudit!.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.Authorized,
            writtenAudit.ReasonCategory);
        Assert.Equal(2, writtenAudit.RowsReturned);
        Assert.Equal(
            "Germany",
            writtenAudit.NormalizedParameters["country"]);

        corporateReader.Verify(
            x => x.ReadSalesSummaryAsync(
                effectiveScope,
                501,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Deny_WritesAuditBeforeAccessDeniedAndDoesNotReadCorporateData()
    {
        var evaluator = new Mock<IAccessPolicyEvaluator>();
        var platformStore = new Mock<IPlatformStore>();
        var corporateReader = new Mock<ICorporateDataReader>();
        var auditWriter = new Mock<IAuditWriter>();

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Deny,
            "sales.read",
            "SalesSummary",
            null,
            AuthorizationReasonCategories.RowScopeNotAllowed);

        platformStore
            .Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource());

        evaluator
            .Setup(x => x.EvaluateAsync(
                It.IsAny<ValidatedTokenContext>(),
                "SalesSummary",
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        AuditRecord? writtenAudit = null;

        auditWriter
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>(
                (record, _) => writtenAudit = record)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            evaluator,
            platformStore,
            corporateReader,
            auditWriter);

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => handler.HandleAsync(
                CreateRequest("USA"),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.AccessDenied,
            exception.Category);

        Assert.NotNull(writtenAudit);
        Assert.Equal("DENY", writtenAudit!.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.RowScopeNotAllowed,
            writtenAudit.ReasonCategory);
        Assert.Equal(0, writtenAudit.RowsReturned);

        corporateReader.Verify(
            x => x.ReadSalesSummaryAsync(
                It.IsAny<RowScope>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ResultLimitExceeded_WritesAllowAuditWithZeroRowsAndThrows()
    {
        var evaluator = new Mock<IAccessPolicyEvaluator>();
        var platformStore = new Mock<IPlatformStore>();
        var corporateReader = new Mock<ICorporateDataReader>();
        var auditWriter = new Mock<IAuditWriter>();

        var effectiveScope = new RowScope(
            RowScopeMode.All,
            new Dictionary<string, IReadOnlySet<string>>());

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "sales.read",
            "SalesSummary",
            effectiveScope,
            AuthorizationReasonCategories.Authorized);

        platformStore
            .Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource());

        evaluator
            .Setup(x => x.EvaluateAsync(
                It.IsAny<ValidatedTokenContext>(),
                "SalesSummary",
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        var rows = (IReadOnlyList<SalesSummaryRow>)Enumerable
            .Range(1, 501)
            .Select(id => new SalesSummaryRow(
                id,
                "Germany",
                new DateOnly(2021, 1, 1),
                1.00m))
            .ToArray();

        corporateReader
            .Setup(x => x.ReadSalesSummaryAsync(
                effectiveScope,
                501,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        AuditRecord? writtenAudit = null;

        auditWriter
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>(
                (record, _) => writtenAudit = record)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            evaluator,
            platformStore,
            corporateReader,
            auditWriter);

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => handler.HandleAsync(
                CreateRequest(),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.ResultLimitExceeded,
            exception.Category);

        Assert.NotNull(writtenAudit);
        Assert.Equal("ALLOW", writtenAudit!.Decision);
        Assert.Equal(
            PdgErrorCategory.ResultLimitExceeded,
            writtenAudit.ReasonCategory);
        Assert.Equal(0, writtenAudit.RowsReturned);
    }

    [Fact]
    public async Task HandleAsync_CorporateDataUnavailable_WritesAllowFailureAuditAndRethrows()
    {
        var evaluator = new Mock<IAccessPolicyEvaluator>();
        var platformStore = new Mock<IPlatformStore>();
        var corporateReader = new Mock<ICorporateDataReader>();
        var auditWriter = new Mock<IAuditWriter>();

        var effectiveScope = new RowScope(
            RowScopeMode.All,
            new Dictionary<string, IReadOnlySet<string>>());

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "sales.read",
            "SalesSummary",
            effectiveScope,
            AuthorizationReasonCategories.Authorized);

        platformStore
            .Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource());

        evaluator
            .Setup(x => x.EvaluateAsync(
                It.IsAny<ValidatedTokenContext>(),
                "SalesSummary",
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        corporateReader
            .Setup(x => x.ReadSalesSummaryAsync(
                effectiveScope,
                501,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new PdgException(
                    PdgErrorCategory.CorporateDataSourceUnavailable));

        AuditRecord? writtenAudit = null;

        auditWriter
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuditRecord, CancellationToken>(
                (record, _) => writtenAudit = record)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler(
            evaluator,
            platformStore,
            corporateReader,
            auditWriter);

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => handler.HandleAsync(
                CreateRequest(),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.CorporateDataSourceUnavailable,
            exception.Category);

        Assert.NotNull(writtenAudit);
        Assert.Equal("ALLOW", writtenAudit!.Decision);
        Assert.Equal(
            PdgErrorCategory.CorporateDataSourceUnavailable,
            writtenAudit.ReasonCategory);
        Assert.Equal(0, writtenAudit.RowsReturned);
    }

    [Fact]
    public async Task HandleAsync_AllowAuditFails_DoesNotReleaseBufferedResult()
    {
        var evaluator = new Mock<IAccessPolicyEvaluator>();
        var platformStore = new Mock<IPlatformStore>();
        var corporateReader = new Mock<ICorporateDataReader>();
        var auditWriter = new Mock<IAuditWriter>();

        var effectiveScope = new RowScope(
            RowScopeMode.All,
            new Dictionary<string, IReadOnlySet<string>>());

        var decision = new AuthorizationDecision(
            AuthorizationDecisionKind.Allow,
            "sales.read",
            "SalesSummary",
            effectiveScope,
            AuthorizationReasonCategories.Authorized);

        platformStore
            .Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateResource());

        evaluator
            .Setup(x => x.EvaluateAsync(
                It.IsAny<ValidatedTokenContext>(),
                "SalesSummary",
                It.IsAny<IReadOnlyDictionary<string, string?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decision);

        corporateReader
            .Setup(x => x.ReadSalesSummaryAsync(
                effectiveScope,
                501,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlyList<SalesSummaryRow>)new[]
                {
                    new SalesSummaryRow(
                        2,
                        "Germany",
                        new DateOnly(2021, 1, 1),
                        1.98m)
                });

        auditWriter
            .Setup(x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new PdgException(
                    PdgErrorCategory.AuditWriteFailed));

        var handler = CreateHandler(
            evaluator,
            platformStore,
            corporateReader,
            auditWriter);

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => handler.HandleAsync(
                CreateRequest(),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.AuditWriteFailed,
            exception.Category);

        auditWriter.Verify(
            x => x.WriteAsync(
                It.IsAny<AuditRecord>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static GetSalesSummaryHandler CreateHandler(
        Mock<IAccessPolicyEvaluator> evaluator,
        Mock<IPlatformStore> platformStore,
        Mock<ICorporateDataReader> corporateReader,
        Mock<IAuditWriter> auditWriter)
    {
        return new GetSalesSummaryHandler(
            evaluator.Object,
            platformStore.Object,
            corporateReader.Object,
            auditWriter.Object,
            new AuditRecordFactory(
                new FixedClock(FixedTime)));
    }

    private static GetSalesSummaryRequest CreateRequest(
        string? country = null)
    {
        return new GetSalesSummaryRequest(
            new ValidatedTokenContext(
                "user_42",
                "sales_copilot_v1",
                new HashSet<string>(
                    new[] { "sales.read" },
                    StringComparer.Ordinal)),
            "SalesSummary",
            country);
    }

    private static PublishedResource CreateResource()
    {
        return new PublishedResource(
            "SalesSummary",
            "sales.read",
            500,
            new[]
            {
                new ResourceParameter(
                    "country",
                    "string",
                    false)
            },
            new[]
            {
                new ResourceOutputField("CustomerId", 1),
                new ResourceOutputField("Country", 2),
                new ResourceOutputField("InvoiceDate", 3),
                new ResourceOutputField("Total", 4)
            });
    }

    private static RowScope CreateCountryScope(
        string country)
    {
        return new RowScope(
            RowScopeMode.AllowList,
            new Dictionary<string, IReadOnlySet<string>>
            {
                ["country"] = new HashSet<string>(
                    new[] { country },
                    StringComparer.Ordinal)
            });
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