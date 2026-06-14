using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Contracts.Users;
using UserService.Data;

namespace UserService.Features.Users;

public sealed record GetProfileQuery(string UserId) : IRequest<UserProfileDto>;

public sealed class GetProfileHandler(UserDbContext dbContext, IMapper mapper) : IRequestHandler<GetProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found");

        return mapper.Map<UserProfileDto>(profile);
    }
}
