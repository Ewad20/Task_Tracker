using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Contracts.Users;
using UserService.Data;

namespace UserService.Features.Users;

public sealed record UpdateUserRoleCommand(string UserId, UpdateUserRoleRequest Request) : IRequest<UserProfileDto>;

public sealed class UpdateUserRoleHandler(UserDbContext dbContext, IMapper mapper)
    : IRequestHandler<UpdateUserRoleCommand, UserProfileDto>
{
    private static string NormalizeRole(string role)
        => string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "User";

    public async Task<UserProfileDto> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found");

        profile.Role = NormalizeRole(request.Request.Role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.Map<UserProfileDto>(profile);
    }
}
