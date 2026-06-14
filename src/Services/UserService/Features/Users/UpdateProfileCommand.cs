using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserService.Contracts.Users;
using UserService.Data;

namespace UserService.Features.Users;

public sealed record UpdateProfileCommand(string UserId, UpdateProfileRequest Request) : IRequest<UserProfileDto>;

public sealed class UpdateProfileHandler(UserDbContext dbContext, IMapper mapper) : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Profile not found");

        profile.DisplayName = request.Request.DisplayName;
        profile.Bio = request.Request.Bio;

        await dbContext.SaveChangesAsync(cancellationToken);

        return mapper.Map<UserProfileDto>(profile);
    }
}
