namespace UserService.Contracts.Auth;

public sealed record AuthResponse(string Token, DateTime ExpiresAt);
