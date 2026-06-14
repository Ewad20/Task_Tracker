using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Identity;
using UserService.Contracts.Users;
using UserService.Entities;

namespace UserService.Features.Users;

public sealed record ChangePasswordCommand(string UserId, ChangePasswordRequest Request) : IRequest;

public sealed class ChangePasswordHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task<Unit> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found");

        var result = await userManager.ChangePasswordAsync(user, request.Request.CurrentPassword, request.Request.NewPassword);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(message);
        }

        return Unit.Value;
    }
}
