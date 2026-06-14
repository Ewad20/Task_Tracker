using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Contracts.Users;
using UserService.Data;

namespace UserService.Features.Users;

public sealed record ListUsersQuery() : IRequest<IReadOnlyList<UserProfileDto>>;

public sealed class ListUsersHandler(UserDbContext dbContext, IMapper mapper)
    : IRequestHandler<ListUsersQuery, IReadOnlyList<UserProfileDto>>
{
    public async Task<IReadOnlyList<UserProfileDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var profiles = await dbContext.UserProfiles
            .OrderBy(profile => profile.DisplayName)
            .ToListAsync(cancellationToken);

        return profiles.Select(mapper.Map<UserProfileDto>).ToList();
    }
}
