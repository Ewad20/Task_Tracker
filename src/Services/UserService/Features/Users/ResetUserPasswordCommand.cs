using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Identity;
using UserService.Contracts.Users;
using UserService.Entities;

namespace UserService.Features.Users;

public sealed record ResetUserPasswordCommand(string UserId, ResetUserPasswordRequest Request) : IRequest;

public sealed class ResetUserPasswordHandler(
    UserManager<ApplicationUser> userManager,
    IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators)
    : IRequestHandler<ResetUserPasswordCommand>
{
    public async Task<Unit> Handle(ResetUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId)
            ?? throw new InvalidOperationException("User not found");

        foreach (var validator in passwordValidators)
        {
            var validationResult = await validator.ValidateAsync(userManager, user, request.Request.NewPassword);
            if (!validationResult.Succeeded)
            {
                var message = string.Join("; ", validationResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException(message);
            }
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, request.Request.NewPassword);
        await userManager.UpdateSecurityStampAsync(user);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException(message);
        }

        return Unit.Value;
    }
}
