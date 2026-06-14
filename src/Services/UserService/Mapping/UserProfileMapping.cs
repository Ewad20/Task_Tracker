using AutoMapper;
using UserService.Contracts.Users;
using UserService.Entities;

namespace UserService.Mapping;

public sealed class UserProfileMapping : Profile
{
    public UserProfileMapping()
    {
        CreateMap<UserProfile, UserProfileDto>();
    }
}
