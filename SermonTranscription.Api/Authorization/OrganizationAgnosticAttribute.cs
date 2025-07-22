using Microsoft.AspNetCore.Mvc;

namespace SermonTranscription.Api.Authorization;

/// <summary>
/// Marks an endpoint as organization-agnostic - requires authentication but no tenant context
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class OrganizationAgnosticAttribute : Attribute
{
}
