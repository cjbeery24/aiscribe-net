using Microsoft.Extensions.Logging;
using Moq;
using SermonTranscription.Application.Services;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Application.Interfaces;
using SermonTranscription.Domain.Entities;
using SermonTranscription.Domain.Enums;
using SermonTranscription.Domain.Interfaces;
using SermonTranscription.Tests.Unit.Common;
using FluentAssertions;

namespace SermonTranscription.Tests.Unit.Services;

public class OrganizationServiceTests : BaseUnitTest
{
    private readonly Mock<IOrganizationRepository> _mockOrganizationRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserOrganizationRepository> _mockUserOrganizationRepository;
    private readonly Mock<ITranscriptionSessionRepository> _mockTranscriptionSessionRepository;
    private readonly Mock<ITranscriptionRepository> _mockTranscriptionRepository;
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<IUserOrganizationCacheService> _mockUserOrganizationCacheService;
    private readonly Mock<ILogger<OrganizationService>> _mockLogger;
    private readonly OrganizationService _organizationService;

    public OrganizationServiceTests()
    {
        _mockOrganizationRepository = new Mock<IOrganizationRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserOrganizationRepository = new Mock<IUserOrganizationRepository>();
        _mockTranscriptionSessionRepository = new Mock<ITranscriptionSessionRepository>();
        _mockTranscriptionRepository = new Mock<ITranscriptionRepository>();
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockUserOrganizationCacheService = new Mock<IUserOrganizationCacheService>();
        _mockLogger = new Mock<ILogger<OrganizationService>>();

        _organizationService = new OrganizationService(
            _mockOrganizationRepository.Object,
            _mockUserRepository.Object,
            _mockUserOrganizationRepository.Object,
            _mockTranscriptionSessionRepository.Object,
            _mockTranscriptionRepository.Object,
            _mockSubscriptionRepository.Object,
            _mockUserOrganizationCacheService.Object,
            _mockLogger.Object);
    }

    #region CreateOrganizationAsync Tests

    [Fact]
    public async Task CreateOrganizationAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "Test Description",
            ContactEmail = "test@example.com",
            PhoneNumber = "123-456-7890",
            Address = "123 Test St",
            City = "Test City",
            State = "TS",
            PostalCode = "12345",
            Country = "Test Country",
            WebsiteUrl = "https://test.com"
        };

        var createdByUserId = Guid.NewGuid();
        var creatingUser = TestDataFactory.UserFaker.Generate();
        creatingUser.Id = createdByUserId;
        creatingUser.IsActive = true;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync(creatingUser);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ReturnsAsync(new List<Organization>());

        _mockOrganizationRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(false);

        _mockOrganizationRepository
            .Setup(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None))
            .ReturnsAsync((Organization org, CancellationToken token) => org);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(request.Name);
        result.Data.Description.Should().Be(request.Description);
        result.Data.ContactEmail.Should().Be(request.ContactEmail);

        _mockOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithNonExistentUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "Test Description"
        };

        var createdByUserId = Guid.NewGuid();

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Creating user not found");
        _mockOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithInactiveUser_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "Test Description"
        };

        var createdByUserId = Guid.NewGuid();
        var creatingUser = TestDataFactory.UserFaker.Generate();
        creatingUser.Id = createdByUserId;
        creatingUser.IsActive = false;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync(creatingUser);

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Creating user is not active");
        _mockOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithExistingName_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Existing Organization",
            Description = "Test Description"
        };

        var createdByUserId = Guid.NewGuid();
        var creatingUser = TestDataFactory.UserFaker.Generate();
        creatingUser.Id = createdByUserId;
        creatingUser.IsActive = true;

        var existingOrganization = TestDataFactory.OrganizationFaker.Generate();
        existingOrganization.Name = request.Name;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync(creatingUser);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ReturnsAsync(new List<Organization> { existingOrganization });

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be($"An organization with the name '{request.Name}' already exists");
        _mockOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task CreateOrganizationAsync_WithExistingSlug_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "Test Description"
        };

        var createdByUserId = Guid.NewGuid();
        var creatingUser = TestDataFactory.UserFaker.Generate();
        creatingUser.Id = createdByUserId;
        creatingUser.IsActive = true;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync(creatingUser);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ReturnsAsync(new List<Organization>());

        _mockOrganizationRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(true);

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("already exists");
        _mockOrganizationRepository.Verify(x => x.AddAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region GetOrganizationAsync Tests

    [Fact]
    public async Task GetOrganizationAsync_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        // Act
        var result = await _organizationService.GetOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(organizationId);
        result.Data.Name.Should().Be(organization.Name);
    }

    [Fact]
    public async Task GetOrganizationAsync_WithNonExistentId_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.GetOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
    }

    #endregion

    #region GetOrganizationBySlugAsync Tests

    [Fact]
    public async Task GetOrganizationBySlugAsync_WithValidSlug_ShouldReturnSuccess()
    {
        // Arrange
        var slug = "test-organization";
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Slug = slug;

        _mockOrganizationRepository
            .Setup(x => x.GetBySlugAsync(slug, CancellationToken.None))
            .ReturnsAsync(organization);

        // Act
        var result = await _organizationService.GetOrganizationBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Slug.Should().Be(slug);
    }

    [Fact]
    public async Task GetOrganizationBySlugAsync_WithNonExistentSlug_ShouldReturnFailure()
    {
        // Arrange
        var slug = "non-existent-slug";

        _mockOrganizationRepository
            .Setup(x => x.GetBySlugAsync(slug, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.GetOrganizationBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
    }

    #endregion

    #region GetOrganizationsAsync Tests

    [Fact]
    public async Task GetOrganizationsAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new OrganizationSearchRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "test"
        };

        var organizations = TestDataFactory.OrganizationFaker.Generate(5);

        var paginatedResult = new Domain.Common.PaginatedResult<Organization>
        {
            Items = organizations,
            TotalCount = 5,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _mockOrganizationRepository
            .Setup(x => x.GetPaginatedOrganizationsAsync(
                It.IsAny<Domain.Common.PaginationRequest>(),
                request.SearchTerm,
                request.IsActive,
                request.HasActiveSubscription,
                CancellationToken.None))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _organizationService.GetOrganizationsAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Organizations.Should().HaveCount(5);
        result.Data.TotalCount.Should().Be(5);
        result.Data.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetOrganizationsAsync_WithActiveFilter_ShouldUseActiveOrganizations()
    {
        // Arrange
        var request = new OrganizationSearchRequest
        {
            PageNumber = 1,
            PageSize = 10,
            IsActive = true
        };

        var organizations = TestDataFactory.OrganizationFaker.Generate(3);

        var paginatedResult = new Domain.Common.PaginatedResult<Organization>
        {
            Items = organizations,
            TotalCount = 3,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _mockOrganizationRepository
            .Setup(x => x.GetPaginatedOrganizationsAsync(
                It.IsAny<Domain.Common.PaginationRequest>(),
                request.SearchTerm,
                request.IsActive,
                request.HasActiveSubscription,
                CancellationToken.None))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _organizationService.GetOrganizationsAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        _mockOrganizationRepository.Verify(x => x.GetPaginatedOrganizationsAsync(
            It.IsAny<Domain.Common.PaginationRequest>(),
            request.SearchTerm,
            request.IsActive,
            request.HasActiveSubscription,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetOrganizationsAsync_WithInvalidPagination_ShouldUseDefaults()
    {
        // Arrange
        var request = new OrganizationSearchRequest
        {
            PageNumber = 0,
            PageSize = 0
        };

        var organizations = TestDataFactory.OrganizationFaker.Generate(2);

        var paginatedResult = new Domain.Common.PaginatedResult<Organization>
        {
            Items = organizations,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _mockOrganizationRepository
            .Setup(x => x.GetPaginatedOrganizationsAsync(
                It.IsAny<Domain.Common.PaginationRequest>(),
                request.SearchTerm,
                request.IsActive,
                request.HasActiveSubscription,
                CancellationToken.None))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _organizationService.GetOrganizationsAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(10);
    }

    #endregion

    #region UpdateOrganizationAsync Tests

    [Fact]
    public async Task UpdateOrganizationAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var request = new UpdateOrganizationRequest
        {
            Name = "Updated Organization",
            Description = "Updated Description",
            ContactEmail = "updated@example.com"
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ReturnsAsync(new List<Organization>());

        _mockOrganizationRepository
            .Setup(x => x.SlugExistsAsync(It.IsAny<string>(), CancellationToken.None))
            .ReturnsAsync(false);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.UpdateOrganizationAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(request.Name);
        result.Data.Description.Should().Be(request.Description);
        result.Data.ContactEmail.Should().Be(request.ContactEmail);

        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var request = new UpdateOrganizationRequest
        {
            Name = "Updated Organization"
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.UpdateOrganizationAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task UpdateOrganizationAsync_WithConflictingName_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var request = new UpdateOrganizationRequest
        {
            Name = "Conflicting Name"
        };

        var conflictingOrg = TestDataFactory.OrganizationFaker.Generate();
        conflictingOrg.Id = Guid.NewGuid();
        conflictingOrg.Name = request.Name;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ReturnsAsync(new List<Organization> { conflictingOrg });

        // Act
        var result = await _organizationService.UpdateOrganizationAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be($"An organization with the name '{request.Name}' already exists");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region UpdateOrganizationSettingsAsync Tests

    [Fact]
    public async Task UpdateOrganizationSettingsAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var request = new UpdateOrganizationSettingsRequest
        {
            MaxTranscriptionMinutes = 500,
            CanExportTranscriptions = true,
            HasRealtimeTranscription = true
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.UpdateOrganizationSettingsAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.MaxTranscriptionMinutes.Should().Be(request.MaxTranscriptionMinutes!.Value);
        result.Data.CanExportTranscriptions.Should().Be(request.CanExportTranscriptions!.Value);
        result.Data.HasRealtimeTranscription.Should().Be(request.HasRealtimeTranscription!.Value);

        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateOrganizationSettingsAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var request = new UpdateOrganizationSettingsRequest
        {
            MaxTranscriptionMinutes = 500
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.UpdateOrganizationSettingsAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region UpdateOrganizationLogoAsync Tests

    [Fact]
    public async Task UpdateOrganizationLogoAsync_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var request = new UpdateOrganizationLogoRequest
        {
            LogoUrl = "https://example.com/logo.png"
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.UpdateOrganizationLogoAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.LogoUrl.Should().Be(request.LogoUrl);

        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateOrganizationLogoAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var request = new UpdateOrganizationLogoRequest
        {
            LogoUrl = "https://example.com/logo.png"
        };

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.UpdateOrganizationLogoAsync(organizationId, request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region ActivateOrganizationAsync Tests

    [Fact]
    public async Task ActivateOrganizationAsync_WithValidOrganization_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;
        organization.IsActive = false;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.ActivateOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Organization activated successfully");

        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ActivateOrganizationAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.ActivateOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region DeactivateOrganizationAsync Tests

    [Fact]
    public async Task DeactivateOrganizationAsync_WithValidOrganization_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;
        organization.IsActive = true;

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        _mockOrganizationRepository
            .Setup(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _organizationService.DeactivateOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be("Organization deactivated successfully");

        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DeactivateOrganizationAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.DeactivateOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
        _mockOrganizationRepository.Verify(x => x.UpdateAsync(It.IsAny<Organization>(), CancellationToken.None), Times.Never);
    }

    #endregion

    #region GetOrganizationWithUsersAsync Tests

    [Fact]
    public async Task GetOrganizationWithUsersAsync_WithValidOrganization_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var users = TestDataFactory.UserFaker.Generate(3);
        var userOrganizations = users.Select((user, index) => new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = organizationId,
            User = user,
            Role = UserRole.OrganizationUser,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-index)
        }).ToList();

        organization.UserOrganizations = userOrganizations;

        _mockOrganizationRepository
            .Setup(x => x.GetWithUserOrganizationsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        // Act
        var result = await _organizationService.GetOrganizationWithUsersAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Users.Should().HaveCount(3);
        result.Data.Users.Should().AllSatisfy(u => u.UserId.Should().NotBeEmpty());
    }

    [Fact]
    public async Task GetOrganizationWithUsersAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetWithUserOrganizationsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.GetOrganizationWithUsersAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
    }

    #endregion

    #region GetOrganizationWithSubscriptionsAsync Tests

    [Fact]
    public async Task GetOrganizationWithSubscriptionsAsync_WithValidOrganization_ShouldReturnSuccess()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var organization = TestDataFactory.OrganizationFaker.Generate();
        organization.Id = organizationId;

        var subscriptions = TestDataFactory.SubscriptionFaker.Generate(2);
        foreach (var subscription in subscriptions)
        {
            subscription.OrganizationId = organizationId;
        }
        organization.Subscriptions = subscriptions;

        _mockOrganizationRepository
            .Setup(x => x.GetWithSubscriptionsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync(organization);

        // Act
        var result = await _organizationService.GetOrganizationWithSubscriptionsAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Subscriptions.Should().HaveCount(2);
        result.Data.Subscriptions.Should().AllSatisfy(s => s.OrganizationId.Should().Be(organizationId));
    }

    [Fact]
    public async Task GetOrganizationWithSubscriptionsAsync_WithNonExistentOrganization_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetWithSubscriptionsAsync(organizationId, CancellationToken.None))
            .ReturnsAsync((Organization?)null);

        // Act
        var result = await _organizationService.GetOrganizationWithSubscriptionsAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Organization not found");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateOrganizationAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var request = new CreateOrganizationRequest
        {
            Name = "Test Organization",
            Description = "Test Description"
        };

        var createdByUserId = Guid.NewGuid();
        var creatingUser = TestDataFactory.UserFaker.Generate();
        creatingUser.Id = createdByUserId;
        creatingUser.IsActive = true;

        _mockUserRepository
            .Setup(x => x.GetByIdAsync(createdByUserId, CancellationToken.None))
            .ReturnsAsync(creatingUser);

        _mockOrganizationRepository
            .Setup(x => x.SearchByNameAsync(request.Name, CancellationToken.None))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _organizationService.CreateOrganizationAsync(request, createdByUserId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("An error occurred while creating the organization");
    }

    [Fact]
    public async Task GetOrganizationAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        _mockOrganizationRepository
            .Setup(x => x.GetByIdAsync(organizationId, CancellationToken.None))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _organizationService.GetOrganizationAsync(organizationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("An error occurred while retrieving the organization");
    }

    #endregion
}
