using Perimeter.Gateway.Application.Authorization;

namespace Perimeter.Gateway.UnitTests.Authorization;

public sealed class AuthorizationReasonCategoriesTests
{
    [Fact]
    public void Constants_HaveStableContractValues()
    {
        Assert.Equal(
            "authorized",
            AuthorizationReasonCategories.Authorized);

        Assert.Equal(
            "delegation_not_allowed",
            AuthorizationReasonCategories.DelegationNotAllowed);

        Assert.Equal(
            "actor_capability_not_allowed",
            AuthorizationReasonCategories.ActorCapabilityNotAllowed);

        Assert.Equal(
            "subject_resource_not_allowed",
            AuthorizationReasonCategories.SubjectResourceNotAllowed);

        Assert.Equal(
            "required_capability_not_allowed",
            AuthorizationReasonCategories.RequiredCapabilityNotAllowed);

        Assert.Equal(
            "row_scope_not_allowed",
            AuthorizationReasonCategories.RowScopeNotAllowed);

        Assert.Equal(
            "result_limit_exceeded",
            AuthorizationReasonCategories.ResultLimitExceeded);

        Assert.Equal(
            "corporate_data_source_unavailable",
            AuthorizationReasonCategories.CorporateDataSourceUnavailable);
    }
}