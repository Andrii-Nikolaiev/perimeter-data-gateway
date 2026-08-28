namespace Perimeter.Gateway.Application.Errors;

public static class PdgErrorCategory
{
    public const string InvalidRequest = "invalid_request";
    public const string ResultLimitExceeded = "result_limit_exceeded";
    public const string AuthenticationFailed = "authentication_failed";
    public const string AccessDenied = "access_denied";
    public const string ResourceNotFound = "resource_not_found";
    public const string InternalError = "internal_error";
    public const string CorporateDataSourceUnavailable = "corporate_data_source_unavailable";
    public const string PlatformStoreUnavailable = "platform_store_unavailable";
    public const string AuditWriteFailed = "audit_write_failed";
}