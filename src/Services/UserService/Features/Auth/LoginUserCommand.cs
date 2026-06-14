using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Contracts.Auth;
using UserService.Data;
using UserService.Entities;
using UserService.Security;

namespace UserService.Features.Auth;

public sealed record LoginUserCommand(LoginRequest Request) : IRequest<AuthResponse>;

public sealed class LoginUserHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    UserDbContext dbContext,
    IJwtTokenService tokenService)
    : IRequestHandler<LoginUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Request.Email)
            ?? throw new InvalidOperationException("User not found");

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Request.Password, false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Invalid credentials");
        }

        var role = await dbContext.UserProfiles
            .Where(profile => profile.UserId == user.Id)
            .Select(profile => profile.Role)
            .FirstOrDefaultAsync(cancellationToken) ?? "User";

        return tokenService.CreateToken(user, role);
    }
}
