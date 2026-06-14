namespace UserService.Contracts.Users
{
    public sealed record UserProfileDto(Guid Id, string UserId, string DisplayName, string Bio, string Role);
}
