namespace AddressBook.Api;

/// <summary>
/// URL-segment API versioning convention ("/api/v1/..."). A plain constant prefix was chosen
/// over the Asp.Versioning package for simplicity: there is only one version, and this keeps
/// routing trivial to read and to teach (see README "Design" section for the trade-off).
/// </summary>
public static class ApiRoutes
{
    public const string V1 = "api/v1";
}
