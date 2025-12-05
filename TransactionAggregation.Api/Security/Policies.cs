namespace TransactionAggregation.Api.Security;

/// <summary>
/// Defines authorization policy names used throughout the application.
/// </summary>
public static class Policies
{
    /// <summary>
    /// Policy name for requiring an authenticated user.
    /// </summary>
    public const string RequireUser = "RequireUser";

    /// <summary>
    /// Policy name for requiring admin role.
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Policy name for internal service-to-service communication.
    /// </summary>
    public const string InternalService = "InternalService";
}