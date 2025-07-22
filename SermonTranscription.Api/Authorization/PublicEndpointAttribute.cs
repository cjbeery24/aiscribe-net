using Microsoft.AspNetCore.Mvc;

namespace SermonTranscription.Api.Authorization;

/// <summary>
/// Marks an endpoint as public - no authentication or tenant context required
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PublicEndpointAttribute : Attribute
{
}
