using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using UserService.Contracts.Auth;
using UserService.Data;
using UserService.Entities;
using UserService.Messaging;
using UserService.Security;

namespace UserService.Features.Auth;

public sealed record RegisterUserCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public sealed class RegisterUserHandler(
    UserManager<ApplicationUser> userManager,
    UserDbContext dbContext,
    IJwtTokenService tokenService,
    IEventPublisher eventPublisher,
    ILogger<RegisterUserHandler> logger)
    : IRequestHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { UserName = request.Request.Email, Email = request.Request.Email };
        var result = await userManager.CreateAsync(user, request.Request.Password);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(message);
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = request.Request.DisplayName,
            Role = "User"
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            eventPublisher.Publish("users.created", new { user.Id, user.Email, profile.DisplayName, profile.Role });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "User was registered, but publishing users.created event failed.");
        }

        return tokenService.CreateToken(user, profile.Role);
    }
}
