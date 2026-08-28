namespace Perimeter.Gateway.Application.Authorization;

public static class AuthorizationReasonCategories
{
    public const string Authorized = "authorized";
    public const string DelegationNotAllowed = "delegation_not_allowed";
    public const string ActorCapabilityNotAllowed = "actor_capability_not_allowed";
    public const string SubjectResourceNotAllowed = "subject_resource_not_allowed";
    public const string RequiredCapabilityNotAllowed = "required_capability_not_allowed";
    public const string RowScopeNotAllowed = "row_scope_not_allowed";
    public const string ResultLimitExceeded = "result_limit_exceeded";
    public const string CorporateDataSourceUnavailable = "corporate_data_source_unavailable";
}