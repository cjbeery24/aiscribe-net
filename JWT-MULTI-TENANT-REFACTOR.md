# JWT and Multi-Tenant Authorization Refactor

## Overview

This document describes the refactored JWT and multi-tenant authorization approach implemented in the Sermon Transcription API. The new approach follows best practices for multi-tenant applications by separating user identity from tenant context.

## Key Changes

### 1. JWT Token Content

**Before:**

- JWT tokens contained user identity AND tenant-specific information
- Tokens included `organizationId` and `role` claims
- Tokens were tied to a specific organization context

**After:**

- JWT tokens contain ONLY user identity information
- Tokens include: `userId`, `email`, `name`, `jti`, `iat`
- Tokens are organization-agnostic and can be used across multiple organizations

### 2. Tenant Resolution

**Before:**

- Organization context was extracted from JWT claims
- User was tied to a specific organization in the token

**After:**

- Organization context is determined from the `X-Organization-ID` header
- Each request must include the target organization ID in the header
- User can switch between organizations without new tokens

### 3. Authorization Flow

**Before:**

1. JWT token validated
2. Organization ID extracted from token claims
3. Authorization based on role in token

**After:**

1. JWT token validated (user identity only)
2. Organization ID extracted from `X-Organization-ID` header
3. User membership and role verified for the specified organization
4. Authorization based on verified membership and role

## Implementation Details

### JWT Service Changes

- `GenerateAccessToken(User user)` - Only requires user, no organization context
- `ValidateToken(string token)` - Returns only user identity information
- Removed `GetOrganizationIdFromToken()` method
- `JwtUserInfo` now only contains `UserId`, `Email`, and `ExpiresAt`

### Tenant Middleware

- Extracts organization ID from `X-Organization-ID` header
- Verifies user membership in the specified organization
- Checks if user is active in the organization
- Creates `TenantContext` with user, organization, and role information
- Stores context in `HttpContext.Items["TenantContext"]`
- **Returns 403 Forbidden** if tenant context cannot be resolved (prevents requests from reaching controllers)
- **Handles three types of endpoints:**
  - **Public endpoints** (no auth, no tenant): login, register, health checks, etc.
  - **Organization-agnostic endpoints** (auth required, no tenant): `/auth/organizations`
  - **Organization-specific endpoints** (auth + tenant required): all other protected endpoints

### Authorization System

- **Simplified Authorization**: Removed complex `OrganizationAuthorizationHandler`
- **TenantMiddleware Handles Everything**: All organization validation happens in middleware
- **Simple Policy Checks**: Authorization policies use `TenantContext` extension methods
- **No Duplicate Database Calls**: Single validation in middleware, reused in policies

### API Endpoints

#### New Endpoint

- `GET /api/v1/auth/organizations` - Returns user's available organizations
- **Organization-agnostic** - works without `X-Organization-ID` header
- Helps frontend know which organizations user can switch to
- **Solves the chicken-and-egg problem** of discovering organizations

#### Updated Endpoints

- All protected endpoints now require `X-Organization-ID` header
- Login/refresh responses no longer include organization-specific information
- Organization context is determined per-request
- Controllers use `TenantContext` extension methods instead of manual header parsing
- Proper authorization policies applied (e.g., `CanManageUsers` for invitation endpoints)
- **No redundant null checks** - middleware ensures valid context before reaching controllers

## Usage Examples

### Frontend Implementation

```javascript
// After login, get user's organizations
const organizations = await api.get("/auth/organizations");

// Switch to a different organization
const response = await api.get("/organizations/profile", {
  headers: {
    "X-Organization-ID": organizationId,
  },
});
```

### API Requests

```bash
# All requests must include X-Organization-ID header
curl -H "Authorization: Bearer <jwt_token>" \
     -H "X-Organization-ID: <organization_id>" \
     https://api.example.com/organizations/profile
```

## Benefits

1. **Flexibility**: Users can switch between organizations without new tokens
2. **Security**: Organization context is verified on each request
3. **Scalability**: Tokens are smaller and contain less sensitive information
4. **Best Practice**: Follows industry standards for multi-tenant applications
5. **Audit Trail**: Clear separation between user identity and tenant context
6. **Performance**: No duplicate database calls - single validation in middleware
7. **Simplicity**: Removed complex authorization handler, uses simple policy checks

## Migration Notes

- Existing tokens will no longer work (they contain organization claims)
- Frontend must be updated to include `X-Organization-ID` header
- New `/auth/organizations` endpoint helps with organization switching
- All authorization policies continue to work as before

## Testing

- Updated unit tests to reflect new JWT service interface
- Integration tests should include `X-Organization-ID` header
- Authorization tests verify organization membership and role checking
