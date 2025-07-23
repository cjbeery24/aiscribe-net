using Microsoft.Extensions.Logging;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Common;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Domain.Exceptions;

namespace SermonTranscription.Application.Services;



public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        ILogger<OrganizationService> logger)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request, Guid createdByUserId)
    {
        try
        {
            // Validate the creating user exists and is active
            var creatingUser = await _userRepository.GetByIdAsync(createdByUserId);
            if (creatingUser == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Creating user not found");
            }

            if (!creatingUser.IsActive)
            {
                return ServiceResult<OrganizationResponse>.Failure("Creating user is not active");
            }

            // Check if organization name already exists
            var existingOrganizations = await _organizationRepository.SearchByNameAsync(request.Name);
            if (existingOrganizations.Any(o => o.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return ServiceResult<OrganizationResponse>.Failure($"An organization with the name '{request.Name}' already exists");
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
            if (await _organizationRepository.SlugExistsAsync(organization.Slug!))
            {
                return ServiceResult<OrganizationResponse>.Failure($"An organization with the slug '{organization.Slug}' already exists");
            }

            // Save organization
            await _organizationRepository.AddAsync(organization);

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
            await _organizationRepository.UpdateAsync(organization);

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

    public async Task<ServiceResult<OrganizationResponse>> GetOrganizationAsync(Guid organizationId)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
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

    public async Task<ServiceResult<OrganizationResponse>> GetOrganizationBySlugAsync(string slug)
    {
        try
        {
            var organization = await _organizationRepository.GetBySlugAsync(slug);
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

    public async Task<ServiceResult<OrganizationListResponse>> GetOrganizationsAsync(OrganizationSearchRequest request)
    {
        try
        {
            // Validate pagination parameters
            if (request.PageNumber < 1) request.PageNumber = 1;
            if (request.PageSize < 1 || request.PageSize > 100) request.PageSize = 10;

            // Get organizations based on search criteria
            IEnumerable<Organization> organizations;

            // Optimize based on filters
            if (request.IsActive == true)
            {
                // Use repository method for active organizations
                organizations = await _organizationRepository.GetActiveOrganizationsAsync();
            }
            else if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                // Use repository method for name search
                organizations = await _organizationRepository.SearchByNameAsync(request.SearchTerm);
            }
            else
            {
                // Get all organizations
                organizations = await _organizationRepository.GetAllAsync();
            }

            // Apply additional filters that can't be done at database level
            var filteredOrganizations = organizations.AsEnumerable();

            if (request.IsActive.HasValue && request.IsActive.Value == false)
            {
                // Filter for inactive organizations (since we already have active ones from repository)
                filteredOrganizations = filteredOrganizations.Where(o => !o.IsActive);
            }

            if (request.HasActiveSubscription.HasValue)
            {
                filteredOrganizations = filteredOrganizations.Where(o => o.HasActiveSubscription() == request.HasActiveSubscription.Value);
            }

            // Apply sorting
            filteredOrganizations = request.SortBy?.ToLower() switch
            {
                "name" => request.SortDescending ? filteredOrganizations.OrderByDescending(o => o.Name) : filteredOrganizations.OrderBy(o => o.Name),
                "createdat" => request.SortDescending ? filteredOrganizations.OrderByDescending(o => o.CreatedAt) : filteredOrganizations.OrderBy(o => o.CreatedAt),
                "activeusercount" => request.SortDescending ? filteredOrganizations.OrderByDescending(o => o.GetActiveUserCount()) : filteredOrganizations.OrderBy(o => o.GetActiveUserCount()),
                _ => filteredOrganizations.OrderBy(o => o.Name)
            };

            // Apply pagination
            var totalCount = filteredOrganizations.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            var pagedOrganizations = filteredOrganizations
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var response = new OrganizationListResponse
            {
                Organizations = pagedOrganizations.Select(MapToOrganizationSummaryDto).ToList(),
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.PageNumber < totalPages,
                HasPreviousPage = request.PageNumber > 1
            };

            return ServiceResult<OrganizationListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations");
            return ServiceResult<OrganizationListResponse>.Failure("An error occurred while retrieving organizations");
        }
    }

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                // Check if new name conflicts with existing organization
                var existingOrganizations = await _organizationRepository.SearchByNameAsync(request.Name);
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
                if (await _organizationRepository.SlugExistsAsync(organization.Slug!))
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

            await _organizationRepository.UpdateAsync(organization);

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

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationSettingsAsync(Guid organizationId, UpdateOrganizationSettingsRequest request)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            // Update settings if provided
            if (request.MaxUsers.HasValue)
                organization.MaxUsers = request.MaxUsers.Value;

            if (request.MaxTranscriptionHours.HasValue)
                organization.MaxTranscriptionHours = request.MaxTranscriptionHours.Value;

            if (request.CanExportTranscriptions.HasValue)
                organization.CanExportTranscriptions = request.CanExportTranscriptions.Value;

            if (request.HasRealtimeTranscription.HasValue)
                organization.HasRealtimeTranscription = request.HasRealtimeTranscription.Value;

            organization.UpdatedAt = DateTime.UtcNow;

            await _organizationRepository.UpdateAsync(organization);

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

    public async Task<ServiceResult<OrganizationResponse>> UpdateOrganizationLogoAsync(Guid organizationId, UpdateOrganizationLogoRequest request)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult<OrganizationResponse>.Failure("Organization not found");
            }

            organization.UpdateLogo(request.LogoUrl);
            await _organizationRepository.UpdateAsync(organization);

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



    public async Task<ServiceResult> ActivateOrganizationAsync(Guid organizationId)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult.Failure("Organization not found");
            }

            organization.Activate();
            await _organizationRepository.UpdateAsync(organization);

            _logger.LogInformation("Organization activated: {OrganizationId}", organizationId);
            return ServiceResult.Success("Organization activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating organization {OrganizationId}", organizationId);
            return ServiceResult.Failure("An error occurred while activating the organization");
        }
    }

    public async Task<ServiceResult> DeactivateOrganizationAsync(Guid organizationId)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                return ServiceResult.Failure("Organization not found");
            }

            organization.Deactivate();
            await _organizationRepository.UpdateAsync(organization);

            _logger.LogInformation("Organization deactivated: {OrganizationId}", organizationId);
            return ServiceResult.Success("Organization deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating organization {OrganizationId}", organizationId);
            return ServiceResult.Failure("An error occurred while deactivating the organization");
        }
    }

    public async Task<ServiceResult<OrganizationWithUsersResponse>> GetOrganizationWithUsersAsync(Guid organizationId)
    {
        try
        {
            var organization = await _organizationRepository.GetWithUserOrganizationsAsync(organizationId);
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

    public async Task<ServiceResult<OrganizationWithSubscriptionsResponse>> GetOrganizationWithSubscriptionsAsync(Guid organizationId)
    {
        try
        {
            var organization = await _organizationRepository.GetWithSubscriptionsAsync(organizationId);
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
            MaxUsers = organization.MaxUsers,
            MaxTranscriptionHours = organization.MaxTranscriptionHours,
            CanExportTranscriptions = organization.CanExportTranscriptions,
            HasRealtimeTranscription = organization.HasRealtimeTranscription,
            DisplayName = organization.DisplayName,
            FullAddress = organization.GetFullAddress(),
            HasCompleteContactInfo = organization.HasCompleteContactInfo(),
            HasActiveSubscription = organization.HasActiveSubscription(),
            ActiveUserCount = organization.GetActiveUserCount(),
            CanAddMoreUsers = organization.CanAddMoreUsers(),
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
            MaxUsers = baseResponse.MaxUsers,
            MaxTranscriptionHours = baseResponse.MaxTranscriptionHours,
            CanExportTranscriptions = baseResponse.CanExportTranscriptions,
            HasRealtimeTranscription = baseResponse.HasRealtimeTranscription,
            DisplayName = baseResponse.DisplayName,
            FullAddress = baseResponse.FullAddress,
            HasCompleteContactInfo = baseResponse.HasCompleteContactInfo,
            HasActiveSubscription = baseResponse.HasActiveSubscription,
            ActiveUserCount = baseResponse.ActiveUserCount,
            CanAddMoreUsers = baseResponse.CanAddMoreUsers,
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
            MaxUsers = baseResponse.MaxUsers,
            MaxTranscriptionHours = baseResponse.MaxTranscriptionHours,
            CanExportTranscriptions = baseResponse.CanExportTranscriptions,
            HasRealtimeTranscription = baseResponse.HasRealtimeTranscription,
            DisplayName = baseResponse.DisplayName,
            FullAddress = baseResponse.FullAddress,
            HasCompleteContactInfo = baseResponse.HasCompleteContactInfo,
            HasActiveSubscription = baseResponse.HasActiveSubscription,
            ActiveUserCount = baseResponse.ActiveUserCount,
            CanAddMoreUsers = baseResponse.CanAddMoreUsers,
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
            MaxUsers = subscription.MaxUsers,
            MaxTranscriptionHours = subscription.MaxTranscriptionHours,
            CanExportTranscriptions = subscription.CanExportTranscriptions,
            HasRealtimeTranscription = subscription.HasRealtimeTranscription,
            HasPrioritySupport = subscription.HasPrioritySupport,
            CurrentUsers = subscription.CurrentUsers,
            TranscriptionHoursUsed = subscription.TranscriptionHoursUsed,
            UsageResetDate = subscription.UsageResetDate,
            IsActive = subscription.IsActive,
            IsExpired = subscription.IsExpired,
            IsCancelled = subscription.IsCancelled,
            RemainingUsers = subscription.RemainingUsers,
            RemainingTranscriptionHours = subscription.RemainingTranscriptionHours,
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
            MaxUsers = organization.MaxUsers,
            HasActiveSubscription = organization.HasActiveSubscription()
        };
    }
}
