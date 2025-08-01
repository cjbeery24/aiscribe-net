using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;
using SermonTranscription.Domain.Common;

namespace SermonTranscription.Application.Services;



public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserOrganizationRepository _userOrganizationRepository;
    private readonly ITranscriptionSessionRepository _transcriptionSessionRepository;
    private readonly ITranscriptionRepository _transcriptionRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUserOrganizationCacheService _userOrganizationCacheService;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUserOrganizationRepository userOrganizationRepository,
        ITranscriptionSessionRepository transcriptionSessionRepository,
        ITranscriptionRepository transcriptionRepository,
        ISubscriptionRepository subscriptionRepository,
        IUserOrganizationCacheService userOrganizationCacheService,
        ILogger<OrganizationService> logger)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _userOrganizationRepository = userOrganizationRepository;
        _transcriptionSessionRepository = transcriptionSessionRepository;
        _transcriptionRepository = transcriptionRepository;
        _subscriptionRepository = subscriptionRepository;
        _userOrganizationCacheService = userOrganizationCacheService;
        _logger = logger;
    }

    public async Task<ServiceResult<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate the creating user exists and is active
            var creatingUser = await _userRepository.GetByIdAsync(createdByUserId, cancellationToken);
            if (creatingUser == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Creating user not found", ErrorCode.NotFound);
            }

            if (!creatingUser.IsActive)
            {
                return ServiceResult<OrganizationResponse>.Failure("Creating user is not active", ErrorCode.Forbidden);
            }

            // Check if organization name already exists
            var existingOrganizations = await _organizationRepository.SearchByNameAsync(request.Name, cancellationToken);
            if (existingOrganizations.Any(o => o.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<OrganizationResponse>.Failure($"An organization with the name '{request.Name}' already exists", ErrorCode.Conflict);
            }

            // Create new organization
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                ContactEmail = request.ContactEmail,
                PhoneNumber = request.PhoneNumber,
                Address = request.Address,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country,
                WebsiteUrl = request.WebsiteUrl,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Generate slug
            organization.UpdateSlug();

            // Check if slug already exists
            if (await _organizationRepository.SlugExistsAsync(organization.Slug!, cancellationToken))
            {
                return ServiceResult<OrganizationResponse>.Failure($"An organization with the slug '{organization.Slug}' already exists", ErrorCode.Conflict);
            }

            // Save organization
            await _organizationRepository.AddAsync(organization, cancellationToken);

            // Create user-organization relationship (user becomes admin)
            var userOrganization = new UserOrganization
            {
                UserId = createdByUserId,
                OrganizationId = organization.Id,
                Role = UserRole.OrganizationAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            // Add the user-organization relationship to the organization
            organization.UserOrganizations.Add(userOrganization);

            // Update the organization to save the user-organization relationship
            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            // Invalidate user cache since user now has a new organization membership
            _userOrganizationCacheService.InvalidateUserCache(createdByUserId);

            _logger.LogInformation("Organization created: {OrganizationId} by user {UserId}", organization.Id, createdByUserId);

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization");
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while creating the organization");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found", ErrorCode.NotFound);
            }

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization {OrganizationId}", organizationId);
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while retrieving the organization");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> GetOrganizationBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetBySlugAsync(slug, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization by slug {Slug}", slug);
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while retrieving the organization");
        }
    }

    public async Task<ServiceResult<OrganizationListResponse>> GetOrganizationsAsync(OrganizationSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create pagination request
            var paginationRequest = new PaginationRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };

            // Get paginated results from repository
            var paginatedResult = await _organizationRepository.GetPaginatedOrganizationsAsync(
                paginationRequest,
                request.SearchTerm,
                request.IsActive,
                request.HasActiveSubscription,
                cancellationToken);

            // Map to DTOs
            var organizations = paginatedResult.Items.Select(MapToOrganizationSummaryDto).ToList();

            var response = new OrganizationListResponse
            {
                Organizations = organizations,
                TotalCount = paginatedResult.TotalCount,
                PageNumber = paginatedResult.PageNumber,
                PageSize = paginatedResult.PageSize,
                TotalPages = paginatedResult.TotalPages,
                HasNextPage = paginatedResult.HasNextPage,
                HasPreviousPage = paginatedResult.HasPreviousPage
            };

            return ServiceResult<OrganizationListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations");
            return ServiceResult<OrganizationListResponse>.Failure("An error occurred while retrieving organizations");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                // Check if new name conflicts with existing organization
                var existingOrganizations = await _organizationRepository.SearchByNameAsync(request.Name, cancellationToken);
                var conflictingOrg = existingOrganizations.FirstOrDefault(o =>
                    o.Id != organizationId &&
                    o.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

                if (conflictingOrg != null)
                {
                    return ServiceResult<OrganizationResponse>.Failure($"An organization with the name '{request.Name}' already exists");
                }

                organization.Name = request.Name;
                organization.UpdateSlug();

                // Check if new slug conflicts with existing organization
                if (await _organizationRepository.SlugExistsAsync(organization.Slug!, cancellationToken))
                {
                    return ServiceResult<OrganizationResponse>.Failure($"An organization with the slug '{organization.Slug}' already exists");
                }
            }

            if (request.Description != null)
                organization.Description = request.Description;

            if (request.ContactEmail != null)
                organization.ContactEmail = request.ContactEmail;

            if (request.PhoneNumber != null)
                organization.PhoneNumber = request.PhoneNumber;

            if (request.Address != null)
                organization.Address = request.Address;

            if (request.City != null)
                organization.City = request.City;

            if (request.State != null)
                organization.State = request.State;

            if (request.PostalCode != null)
                organization.PostalCode = request.PostalCode;

            if (request.Country != null)
                organization.Country = request.Country;

            if (request.WebsiteUrl != null)
                organization.WebsiteUrl = request.WebsiteUrl;

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                    organization.Activate();
                else
                    organization.Deactivate();
            }

            organization.UpdatedAt = DateTime.UtcNow;

            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            _logger.LogInformation("Organization updated: {OrganizationId}", organizationId);

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization {OrganizationId}", organizationId);
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while updating the organization");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationSettingsAsync(Guid organizationId, UpdateOrganizationSettingsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            // Update settings if provided
            if (request.MaxTranscriptionMinutes.HasValue)
                organization.MaxTranscriptionMinutes = request.MaxTranscriptionMinutes.Value;

            if (request.CanExportTranscriptions.HasValue)
                organization.CanExportTranscriptions = request.CanExportTranscriptions.Value;

            if (request.HasRealtimeTranscription.HasValue)
                organization.HasRealtimeTranscription = request.HasRealtimeTranscription.Value;

            organization.UpdatedAt = DateTime.UtcNow;

            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            _logger.LogInformation("Organization settings updated: {OrganizationId}", organizationId);

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization settings {OrganizationId}", organizationId);
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while updating organization settings");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationLogoAsync(Guid organizationId, UpdateOrganizationLogoRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            organization.UpdateLogo(request.LogoUrl);
            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            _logger.LogInformation("Organization logo updated: {OrganizationId}", organizationId);

            var response = MapToOrganizationResponse(organization);
            return ServiceResult<OrganizationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization logo {OrganizationId}", organizationId);
            return ServiceResult<OrganizationResponse>.Failure("An error occurred while updating the organization logo");
        }
    }



    public async Task<ServiceResult> ActivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult.Failure("Organization not found");
            }

            organization.Activate();
            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            _logger.LogInformation("Organization activated: {OrganizationId}", organizationId);
            return ServiceResult.Success("Organization activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating organization {OrganizationId}", organizationId);
            return ServiceResult.Failure("An error occurred while activating the organization");
        }
    }

    public async Task<ServiceResult> DeactivateOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult.Failure("Organization not found");
            }

            organization.Deactivate();
            await _organizationRepository.UpdateAsync(organization, cancellationToken);

            _logger.LogInformation("Organization deactivated: {OrganizationId}", organizationId);
            return ServiceResult.Success("Organization deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating organization {OrganizationId}", organizationId);
            return ServiceResult.Failure("An error occurred while deactivating the organization");
        }
    }

    public async Task<ServiceResult<OrganizationWithUsersResponse>> GetOrganizationWithUsersAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetWithUserOrganizationsAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationWithUsersResponse>.Failure("Organization not found");
            }

            var response = MapToOrganizationWithUsersResponse(organization);
            return ServiceResult<OrganizationWithUsersResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization with users {OrganizationId}", organizationId);
            return ServiceResult<OrganizationWithUsersResponse>.Failure("An error occurred while retrieving the organization");
        }
    }

    public async Task<ServiceResult<OrganizationWithSubscriptionsResponse>> GetOrganizationWithSubscriptionsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var organization = await _organizationRepository.GetWithSubscriptionsAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationWithSubscriptionsResponse>.Failure("Organization not found");
            }

            var response = MapToOrganizationWithSubscriptionsResponse(organization);
            return ServiceResult<OrganizationWithSubscriptionsResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organization with subscriptions {OrganizationId}", organizationId);
            return ServiceResult<OrganizationWithSubscriptionsResponse>.Failure("An error occurred while retrieving the organization");
        }
    }

    private static OrganizationResponse MapToOrganizationResponse(Organization organization)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            ContactEmail = organization.ContactEmail,
            PhoneNumber = organization.PhoneNumber,
            Address = organization.Address,
            City = organization.City,
            State = organization.State,
            PostalCode = organization.PostalCode,
            Country = organization.Country,
            LogoUrl = organization.LogoUrl,
            WebsiteUrl = organization.WebsiteUrl,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt,
            IsActive = organization.IsActive,
            MaxTranscriptionMinutes = organization.MaxTranscriptionMinutes,
            CanExportTranscriptions = organization.CanExportTranscriptions,
            HasRealtimeTranscription = organization.HasRealtimeTranscription,
            DisplayName = organization.DisplayName,
            FullAddress = organization.GetFullAddress(),
            HasCompleteContactInfo = organization.HasCompleteContactInfo(),
            HasActiveSubscription = organization.HasActiveSubscription(),
            ActiveUserCount = organization.GetActiveUserCount(),
            CanCreateTranscription = organization.CanCreateTranscription(),
            HasRealtimeTranscriptionEnabled = organization.HasRealtimeTranscriptionEnabled(),
            CanExportTranscriptionsEnabled = organization.CanExportTranscriptionsEnabled()
        };
    }

    private static OrganizationWithUsersResponse MapToOrganizationWithUsersResponse(Organization organization)
    {
        var baseResponse = MapToOrganizationResponse(organization);

        var response = new OrganizationWithUsersResponse
        {
            // Copy all properties from base response
            Id = baseResponse.Id,
            Name = baseResponse.Name,
            Slug = baseResponse.Slug,
            Description = baseResponse.Description,
            ContactEmail = baseResponse.ContactEmail,
            PhoneNumber = baseResponse.PhoneNumber,
            Address = baseResponse.Address,
            City = baseResponse.City,
            State = baseResponse.State,
            PostalCode = baseResponse.PostalCode,
            Country = baseResponse.Country,
            LogoUrl = baseResponse.LogoUrl,
            WebsiteUrl = baseResponse.WebsiteUrl,
            CreatedAt = baseResponse.CreatedAt,
            UpdatedAt = baseResponse.UpdatedAt,
            IsActive = baseResponse.IsActive,
            MaxTranscriptionMinutes = baseResponse.MaxTranscriptionMinutes,
            CanExportTranscriptions = baseResponse.CanExportTranscriptions,
            HasRealtimeTranscription = baseResponse.HasRealtimeTranscription,
            DisplayName = baseResponse.DisplayName,
            FullAddress = baseResponse.FullAddress,
            HasCompleteContactInfo = baseResponse.HasCompleteContactInfo,
            HasActiveSubscription = baseResponse.HasActiveSubscription,
            ActiveUserCount = baseResponse.ActiveUserCount,
            CanCreateTranscription = baseResponse.CanCreateTranscription,
            HasRealtimeTranscriptionEnabled = baseResponse.HasRealtimeTranscriptionEnabled,
            CanExportTranscriptionsEnabled = baseResponse.CanExportTranscriptionsEnabled,

            // Add user information
            Users = organization.UserOrganizations
                .Where(uo => uo.IsActive)
                .Select(uo => new OrganizationUserResponse
                {
                    UserId = uo.User.Id,
                    Email = uo.User.Email,
                    FirstName = uo.User.FirstName,
                    LastName = uo.User.LastName,
                    Role = uo.Role.ToString(),
                    IsActive = uo.IsActive,
                    JoinedAt = uo.CreatedAt,
                    LastLoginAt = uo.User.LastLoginAt,
                    IsEmailVerified = uo.User.IsEmailVerified,
                    FullName = uo.User.FullName,
                    CanManageUsers = uo.CanManageUsers(),
                    CanManageTranscriptions = uo.CanManageTranscriptions(),
                    CanViewTranscriptions = uo.CanViewTranscriptions(),
                    CanExportTranscriptions = uo.CanExportTranscriptions()
                })
                .ToList()
        };

        return response;
    }

    private static OrganizationWithSubscriptionsResponse MapToOrganizationWithSubscriptionsResponse(Organization organization)
    {
        var baseResponse = MapToOrganizationResponse(organization);

        var response = new OrganizationWithSubscriptionsResponse
        {
            // Copy all properties from base response
            Id = baseResponse.Id,
            Name = baseResponse.Name,
            Slug = baseResponse.Slug,
            Description = baseResponse.Description,
            ContactEmail = baseResponse.ContactEmail,
            PhoneNumber = baseResponse.PhoneNumber,
            Address = baseResponse.Address,
            City = baseResponse.City,
            State = baseResponse.State,
            PostalCode = baseResponse.PostalCode,
            Country = baseResponse.Country,
            LogoUrl = baseResponse.LogoUrl,
            WebsiteUrl = baseResponse.WebsiteUrl,
            CreatedAt = baseResponse.CreatedAt,
            UpdatedAt = baseResponse.UpdatedAt,
            IsActive = baseResponse.IsActive,
            MaxTranscriptionMinutes = baseResponse.MaxTranscriptionMinutes,
            CanExportTranscriptions = baseResponse.CanExportTranscriptions,
            HasRealtimeTranscription = baseResponse.HasRealtimeTranscription,
            DisplayName = baseResponse.DisplayName,
            FullAddress = baseResponse.FullAddress,
            HasCompleteContactInfo = baseResponse.HasCompleteContactInfo,
            HasActiveSubscription = baseResponse.HasActiveSubscription,
            ActiveUserCount = baseResponse.ActiveUserCount,
            CanCreateTranscription = baseResponse.CanCreateTranscription,
            HasRealtimeTranscriptionEnabled = baseResponse.HasRealtimeTranscriptionEnabled,
            CanExportTranscriptionsEnabled = baseResponse.CanExportTranscriptionsEnabled,

            // Add subscription information
            Subscriptions = organization.Subscriptions
                .Select(MapToSubscriptionResponse)
                .ToList(),

            // Set active subscription
            ActiveSubscription = organization.Subscriptions
                .FirstOrDefault(s => s.IsActive) is Subscription activeSub
                ? MapToSubscriptionResponse(activeSub)
                : null
        };

        return response;
    }

    private static SubscriptionResponse MapToSubscriptionResponse(Domain.Entities.Subscription subscription)
    {
        return new SubscriptionResponse
        {
            Id = subscription.Id,
            OrganizationId = subscription.OrganizationId,
            Plan = subscription.Plan,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            CancelledAt = subscription.CancelledAt,
            CreatedAt = subscription.CreatedAt,
            UpdatedAt = subscription.UpdatedAt,
            MonthlyPrice = subscription.MonthlyPrice,
            YearlyPrice = subscription.YearlyPrice,
            Currency = subscription.Currency,
            NextBillingDate = subscription.NextBillingDate,
            LastBillingDate = subscription.LastBillingDate,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            StripeCustomerId = subscription.StripeCustomerId,
            StripePriceId = subscription.StripePriceId,
            MaxTranscriptionMinutes = subscription.MaxTranscriptionMinutes,
            CanExportTranscriptions = subscription.CanExportTranscriptions,
            HasRealtimeTranscription = subscription.HasRealtimeTranscription,
            HasPrioritySupport = subscription.HasPrioritySupport,
            CurrentUsers = subscription.CurrentUsers,
            TranscriptionMinutesUsed = subscription.TranscriptionMinutesUsed,
            UsageResetDate = subscription.UsageResetDate,
            IsActive = subscription.IsActive,
            IsExpired = subscription.IsExpired,
            IsCancelled = subscription.IsCancelled,
            RemainingTranscriptionMinutes = subscription.RemainingTranscriptionMinutes,
            PlanName = subscription.Plan.ToString()
        };
    }

    private static OrganizationSummaryDto MapToOrganizationSummaryDto(Organization organization)
    {
        return new OrganizationSummaryDto
        {
            Id = organization.Id,
            Name = organization.Name,
            Slug = organization.Slug,
            Description = organization.Description,
            LogoUrl = organization.LogoUrl,
            CreatedAt = organization.CreatedAt,
            IsActive = organization.IsActive,
            ActiveUserCount = organization.GetActiveUserCount(),
            HasActiveSubscription = organization.HasActiveSubscription()
        };
    }

    public async Task<ServiceResult<OrganizationDashboardResponse>> GetOrganizationDashboardAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get organization
            var organization = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
            if (organization == null)
            {
                return ServiceResult<OrganizationDashboardResponse>.Failure($"Organization with ID {organizationId} not found");
            }

            // Get all data in parallel for better performance
            var organizationUsersTask = _userOrganizationRepository.GetOrganizationUsersAsync(organizationId, cancellationToken);
            var pendingInvitationsTask = _userOrganizationRepository.GetOrganizationPendingInvitationsAsync(organizationId, cancellationToken);
            var activeSubscriptionTask = _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken);
            var totalUsageTask = _subscriptionRepository.GetTotalUsageMinutesAsync(organizationId, null, cancellationToken);
            var allSessionsTask = _transcriptionSessionRepository.GetByOrganizationAsync(organizationId, cancellationToken);
            var activeSessionsTask = _transcriptionSessionRepository.GetActiveSessionsAsync(cancellationToken);
            var recentSessionsTask = _transcriptionSessionRepository.GetRecentSessionsAsync(organizationId, 5, cancellationToken);
            var allTranscriptionsTask = _transcriptionRepository.GetByOrganizationAsync(organizationId, cancellationToken);
            var recentTranscriptionsTask = _transcriptionRepository.GetRecentTranscriptionsAsync(organizationId, 5, cancellationToken);
            var totalDurationTask = _transcriptionRepository.GetTotalDurationAsync(organizationId, null, cancellationToken);

            await Task.WhenAll(
                organizationUsersTask, pendingInvitationsTask, activeSubscriptionTask, totalUsageTask,
                allSessionsTask, activeSessionsTask, recentSessionsTask, allTranscriptionsTask,
                recentTranscriptionsTask, totalDurationTask);

            var organizationUsers = await organizationUsersTask;
            var pendingInvitations = await pendingInvitationsTask;
            var activeSubscription = await activeSubscriptionTask;
            var totalUsage = await totalUsageTask;
            var allSessions = await allSessionsTask;
            var activeSessions = await activeSessionsTask;
            var recentSessions = await recentSessionsTask;
            var allTranscriptions = await allTranscriptionsTask;
            var recentTranscriptions = await recentTranscriptionsTask;
            var totalDuration = await totalDurationTask;

            // Calculate date ranges
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var sevenDaysAgo = now.AddDays(-7);

            // Build dashboard response
            var dashboard = new OrganizationDashboardResponse
            {
                Overview = new OrganizationOverviewDto
                {
                    OrganizationId = organization.Id,
                    Name = organization.Name,
                    IsActive = organization.IsActive,
                    CreatedAt = organization.CreatedAt,
                    TotalUsers = organizationUsers.Count(),
                    ActiveUsersLast30Days = organizationUsers.Count(uo => uo.User?.LastLoginAt >= thirtyDaysAgo),
                    TotalSessions = allSessions.Count(),
                    TotalTranscriptions = allTranscriptions.Count()
                },

                UserActivity = new UserActivityDto
                {
                    TotalUsers = organizationUsers.Count(),
                    ActiveUsersLast7Days = organizationUsers.Count(uo => uo.User?.LastLoginAt >= sevenDaysAgo),
                    ActiveUsersLast30Days = organizationUsers.Count(uo => uo.User?.LastLoginAt >= thirtyDaysAgo),
                    AdminUsers = organizationUsers.Count(uo => uo.Role == UserRole.OrganizationAdmin),
                    RegularUsers = organizationUsers.Count(uo => uo.Role == UserRole.OrganizationUser),
                    PendingInvitations = pendingInvitations.Count(),
                    RecentUserActivity = organizationUsers
                        .Where(uo => uo.User?.LastLoginAt != null)
                        .OrderByDescending(uo => uo.User!.LastLoginAt)
                        .Take(5)
                        .Select(uo => new UserActivityItemDto
                        {
                            UserId = uo.UserId,
                            FullName = $"{uo.User!.FirstName} {uo.User.LastName}",
                            Email = uo.User.Email,
                            Role = uo.Role.ToString(),
                            LastLoginAt = uo.User.LastLoginAt,
                            IsActive = uo.IsActive
                        })
                        .ToList()
                },

                SubscriptionStatus = new SubscriptionStatusDto
                {
                    CurrentPlan = activeSubscription?.Plan.ToString() ?? "No Plan",
                    Status = activeSubscription?.Status.ToString() ?? "Inactive",
                    MonthlyLimit = activeSubscription?.MaxTranscriptionMinutes ?? 0,
                    MinutesUsed = activeSubscription?.TranscriptionMinutesUsed ?? 0,
                    MinutesRemaining = activeSubscription?.RemainingTranscriptionMinutes ?? 0,
                    UsagePercentage = activeSubscription?.MaxTranscriptionMinutes > 0
                        ? (decimal)activeSubscription.TranscriptionMinutesUsed / activeSubscription.MaxTranscriptionMinutes * 100
                        : 0,
                    IsNearLimit = activeSubscription?.RemainingTranscriptionMinutes <= 120,
                    UsageResetDate = activeSubscription?.UsageResetDate ?? now.AddMonths(1),
                    TotalUsage = totalUsage
                },

                TranscriptionStats = new TranscriptionStatsDto
                {
                    TotalSessions = allSessions.Count(),
                    SessionsLast30Days = allSessions.Count(s => s.CreatedAt >= thirtyDaysAgo),
                    TotalTranscriptions = allTranscriptions.Count(),
                    TranscriptionsLast30Days = allTranscriptions.Count(t => t.ProcessedAt >= thirtyDaysAgo),
                    TotalTranscriptionMinutes = (int)totalDuration.TotalMinutes,
                    AverageSessionDuration = allSessions.Any()
                        ? Math.Round((decimal)(totalDuration.TotalMinutes / allSessions.Count()), 2)
                        : 0,
                    ActiveSessions = activeSessions.Count(s => s.OrganizationId == organizationId),
                    MostActiveSpeaker = allTranscriptions
                        .GroupBy(t => t.Speaker)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key
                },

                RecentActivity = new RecentActivityDto
                {
                    RecentSessions = recentSessions.Select(s => new RecentSessionDto
                    {
                        SessionId = s.Id,
                        Title = s.Title,
                        Status = s.Status.ToString(),
                        DurationMinutes = s.Duration?.Minutes ?? 0,
                        CreatedAt = s.CreatedAt,
                        CreatedBy = $"{s.CreatedByUser?.FirstName} {s.CreatedByUser?.LastName}".Trim()
                    }).ToList(),

                    RecentTranscriptions = recentTranscriptions.Select(t => new RecentTranscriptionDto
                    {
                        TranscriptionId = t.Id,
                        Title = t.Title,
                        Speaker = t.Speaker ?? string.Empty,
                        DurationMinutes = t.DurationSeconds.GetValueOrDefault(0) / 60,
                        ProcessedAt = t.ProcessedAt ?? DateTime.UtcNow,
                        CreatedBy = $"{t.CreatedByUser?.FirstName} {t.CreatedByUser?.LastName}".Trim()
                    }).ToList(),

                    RecentUserActivities = organizationUsers
                        .Where(uo => uo.User?.LastLoginAt != null)
                        .OrderByDescending(uo => uo.User!.LastLoginAt)
                        .Take(5)
                        .Select(uo => new RecentUserActivityDto
                        {
                            UserId = uo.UserId,
                            FullName = $"{uo.User!.FirstName} {uo.User.LastName}",
                            ActivityType = "Login",
                            Description = $"User logged in",
                            ActivityDate = uo.User.LastLoginAt!.Value
                        })
                        .ToList()
                }
            };

            _logger.LogInformation("Dashboard data retrieved for organization {OrganizationId}", organizationId);

            return ServiceResult<OrganizationDashboardResponse>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data for organization {OrganizationId}", organizationId);
            return ServiceResult<OrganizationDashboardResponse>.Failure($"Error retrieving dashboard data: {ex.Message}");
        }
    }
}
