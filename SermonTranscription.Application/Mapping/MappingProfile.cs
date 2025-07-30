using AutoMapper;
using SermonTranscription.Application.DTOs;
using SermonTranscription.Domain.Entities;

namespace SermonTranscription.Application.Mapping;

/// <summary>
/// AutoMapper profile for entity to DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User to UserProfileResponse mapping
        CreateMap<User, UserProfileResponse>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

        // UserOrganization to OrganizationUserResponse mapping
        CreateMap<UserOrganization, OrganizationUserResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.JoinedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.User.LastLoginAt))
            .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.User.IsEmailVerified))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"))
            .ForMember(dest => dest.CanManageUsers, opt => opt.MapFrom(src => src.CanManageUsers()))
            .ForMember(dest => dest.CanViewTranscriptions, opt => opt.MapFrom(src => src.CanViewTranscriptions()))
            .ForMember(dest => dest.CanExportTranscriptions, opt => opt.MapFrom(src => src.CanExportTranscriptions()))
            .ForMember(dest => dest.CanManageTranscriptions, opt => opt.MapFrom(src => src.CanManageTranscriptions()));

        // Organization to OrganizationResponse mapping
        CreateMap<Organization, OrganizationResponse>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // Subscription to SubscriptionResponse mapping
        CreateMap<Subscription, SubscriptionResponse>()
            .ForMember(dest => dest.OrganizationId, opt => opt.MapFrom(src => src.OrganizationId))
            .ForMember(dest => dest.Plan, opt => opt.MapFrom(src => src.Plan))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.ToString()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired))
            .ForMember(dest => dest.IsCancelled, opt => opt.MapFrom(src => src.IsCancelled))
            .ForMember(dest => dest.RemainingTranscriptionMinutes, opt => opt.MapFrom(src => src.RemainingTranscriptionMinutes));
    }
}
