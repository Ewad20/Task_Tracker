using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserService.Entities;
using UserService.Security;

namespace UserService.Tests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(int expiryMinutes = 60)
    {
        var settings = Options.Create(new JwtSettings
        {
            Issuer = "tasktracker",
            Audience = "tasktracker",
            Key = "test-secret-key-must-be-at-least-32-chars",
            ExpiryMinutes = expiryMinutes
        });
        return new JwtTokenService(settings);
    }

    private static ApplicationUser CreateUser(string? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com"
        };

    [Fact]
    public void CreateToken_ReturnsNonEmptyToken()
    {
        var service = CreateService();
        var user = CreateUser();

        var result = service.CreateToken(user, "User");

        result.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateToken_ExpiresAtIsInFuture()
    {
        var service = CreateService(expiryMinutes: 60);
        var user = CreateUser();

        var result = service.CreateToken(user, "User");

        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(2));
    }

    [Fact]
    public void CreateToken_DifferentRoles_ProduceDifferentTokens()
    {
        var service = CreateService();
        var user = CreateUser("same-id");

        var userToken = service.CreateToken(user, "User");
        var adminToken = service.CreateToken(user, "Admin");

        userToken.Token.Should().NotBe(adminToken.Token);
    }

    [Fact]
    public void CreateToken_TokenContainsUserIdClaim()
    {
        var service = CreateService();
        var userId = Guid.NewGuid().ToString();
        var user = CreateUser(userId);

        var result = service.CreateToken(user, "User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
    }

    [Fact]
    public void CreateToken_TokenContainsRoleClaim()
    {
        var service = CreateService();
        var user = CreateUser();

        var result = service.CreateToken(user, "Admin");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public void CreateToken_TokenIssuerAndAudienceMatchConfig()
    {
        var service = CreateService();
        var user = CreateUser();

        var result = service.CreateToken(user, "User");

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Issuer.Should().Be("tasktracker");
        jwt.Audiences.Should().Contain("tasktracker");
    }
}
