using Moq;
using Perimeter.Gateway.Application.Abstractions;
using Perimeter.Gateway.Application.Authorization;
using Perimeter.Gateway.Application.Errors;
using Perimeter.Gateway.Domain.Models;

namespace Perimeter.Gateway.UnitTests.Authorization;

public sealed class AuthorizationPolicyEvaluatorTests
{
    [Fact]
    public async Task EvaluateAsync_AllowListWithoutCountry_ReturnsFullAllowedScope()
    {
        var store = CreateBaseStore();
        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Allow, result.Decision);
        Assert.Equal(AuthorizationReasonCategories.Authorized, result.ReasonCategory);
        Assert.NotNull(result.EffectiveRowScope);
        Assert.Equal(RowScopeMode.AllowList, result.EffectiveRowScope.Mode);

        var countries = result.EffectiveRowScope.Dimensions["country"];

        Assert.True(countries.SetEquals(new[] { "Germany", "France" }));
    }

    [Fact]
    public async Task EvaluateAsync_AllowListWithPermittedCountry_NarrowsEffectiveScope()
    {
        var store = CreateBaseStore();
        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var parameters = new Dictionary<string, string?>
        {
            ["country"] = "Germany"
        };

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            parameters,
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Allow, result.Decision);
        Assert.NotNull(result.EffectiveRowScope);
        Assert.Equal(RowScopeMode.AllowList, result.EffectiveRowScope.Mode);

        var countries = result.EffectiveRowScope.Dimensions["country"];

        Assert.True(countries.SetEquals(new[] { "Germany" }));
    }

    [Fact]
    public async Task EvaluateAsync_OutOfScopeCountry_ReturnsRowScopeDeny()
    {
        var store = CreateBaseStore();
        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var parameters = new Dictionary<string, string?>
        {
            ["country"] = "USA"
        };

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            parameters,
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Deny, result.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.RowScopeNotAllowed,
            result.ReasonCategory);
        Assert.Null(result.EffectiveRowScope);
    }

    [Fact]
    public async Task EvaluateAsync_AllScopeWithoutCountry_ReturnsAllScope()
    {
        var store = CreateBaseStore(
            subjectId: "user_43",
            rowScopeMode: RowScopeMode.All);

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_43", "sales.read");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Allow, result.Decision);
        Assert.NotNull(result.EffectiveRowScope);
        Assert.Equal(RowScopeMode.All, result.EffectiveRowScope.Mode);
        Assert.Empty(result.EffectiveRowScope.Dimensions);
    }

    [Fact]
    public async Task EvaluateAsync_InactiveDelegation_ReturnsDelegationDenyAndStops()
    {
        var store = CreateBaseStore();

        store.Setup(x => x.GetDelegationAsync(
                "user_42",
                "sales_copilot_v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectActorDelegation(
                "user_42",
                "sales_copilot_v1",
                false));

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Deny, result.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.DelegationNotAllowed,
            result.ReasonCategory);

        store.Verify(
            x => x.GetActorAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_MissingRequiredCapability_ReturnsRequiredCapabilityDeny()
    {
        var store = CreateBaseStore();
        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Deny, result.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.RequiredCapabilityNotAllowed,
            result.ReasonCategory);
    }

    [Fact]
    public async Task EvaluateAsync_ActorPolicyDoesNotAllowCapability_ReturnsActorCapabilityDeny()
    {
        var store = CreateBaseStore();

        store.Setup(x => x.GetActorCapabilitiesAsync(
                "sales_copilot_v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlySet<string>)new HashSet<string>(
                    StringComparer.Ordinal));

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Deny, result.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.ActorCapabilityNotAllowed,
            result.ReasonCategory);
    }

    [Fact]
    public async Task EvaluateAsync_SubjectResourceNotAllowed_ReturnsResourceDeny()
    {
        var store = CreateBaseStore(subjectId: "user_44");

        store.Setup(x => x.GetSubjectResourcePermissionAsync(
                "user_44",
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectResourcePermission(
                "user_44",
                "SalesSummary",
                false,
                RowScopeMode.All));

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_44", "sales.read");

        var result = await evaluator.EvaluateAsync(
            token,
            "SalesSummary",
            EmptyParameters(),
            CancellationToken.None);

        Assert.Equal(AuthorizationDecisionKind.Deny, result.Decision);
        Assert.Equal(
            AuthorizationReasonCategories.SubjectResourceNotAllowed,
            result.ReasonCategory);
    }

    [Fact]
    public async Task EvaluateAsync_UnknownResource_ThrowsResourceNotFound()
    {
        var store = CreateBaseStore();

        store.Setup(x => x.GetPublishedResourceAsync(
                "UnknownResource",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublishedResource?)null);

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => evaluator.EvaluateAsync(
                token,
                "UnknownResource",
                EmptyParameters(),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.ResourceNotFound,
            exception.Category);
    }

    [Fact]
    public async Task EvaluateAsync_AllowListWithoutStoredValues_ThrowsInternalError()
    {
        var store = CreateBaseStore();

        store.Setup(x => x.GetSubjectRowScopeValuesAsync(
                "user_42",
                "SalesSummary",
                "country",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlySet<string>)new HashSet<string>(
                    StringComparer.Ordinal));

        var evaluator = new AuthorizationPolicyEvaluator(store.Object);
        var token = CreateToken("user_42", "sales.read");

        var exception = await Assert.ThrowsAsync<PdgException>(
            () => evaluator.EvaluateAsync(
                token,
                "SalesSummary",
                EmptyParameters(),
                CancellationToken.None));

        Assert.Equal(
            PdgErrorCategory.InternalError,
            exception.Category);
    }

    private static Mock<IPlatformStore> CreateBaseStore(
        string subjectId = "user_42",
        RowScopeMode rowScopeMode = RowScopeMode.AllowList)
    {
        var store = new Mock<IPlatformStore>();

        store.Setup(x => x.GetDelegationAsync(
                subjectId,
                "sales_copilot_v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectActorDelegation(
                subjectId,
                "sales_copilot_v1",
                true));

        store.Setup(x => x.GetActorAsync(
                "sales_copilot_v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Actor(
                "sales_copilot_v1",
                "ai_assistant"));

        store.Setup(x => x.GetActorCapabilitiesAsync(
                "sales_copilot_v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlySet<string>)new HashSet<string>(
                    new[] { "sales.read" },
                    StringComparer.Ordinal));

        store.Setup(x => x.GetSubjectAsync(
                subjectId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject(
                subjectId,
                "TestRole"));

        store.Setup(x => x.GetPublishedResourceAsync(
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSalesSummaryResource());

        store.Setup(x => x.GetSubjectResourcePermissionAsync(
                subjectId,
                "SalesSummary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubjectResourcePermission(
                subjectId,
                "SalesSummary",
                true,
                rowScopeMode));

        store.Setup(x => x.GetSubjectRowScopeValuesAsync(
                subjectId,
                "SalesSummary",
                "country",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (IReadOnlySet<string>)new HashSet<string>(
                    new[] { "Germany", "France" },
                    StringComparer.Ordinal));

        return store;
    }

    private static PublishedResource CreateSalesSummaryResource()
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

    private static ValidatedTokenContext CreateToken(
        string subjectId,
        params string[] scopes)
    {
        return new ValidatedTokenContext(
            subjectId,
            "sales_copilot_v1",
            new HashSet<string>(
                scopes,
                StringComparer.Ordinal));
    }

    private static IReadOnlyDictionary<string, string?> EmptyParameters()
    {
        return new Dictionary<string, string?>();
    }
}