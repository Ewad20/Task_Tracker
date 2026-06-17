using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Contracts.Auth;
using UserService.Data;
using UserService.Entities;
using UserService.Features.Auth;
using UserService.Messaging;
using UserService.Security;

namespace UserService.Tests.Features;

public class RegisterUserHandlerTests
{
    private static UserDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new UserDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAuthResponseWithToken()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var userId = Guid.NewGuid().ToString();

        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => user.Id = userId);

        var tokenServiceMock = new Mock<IJwtTokenService>();
        tokenServiceMock
            .Setup(t => t.CreateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns(new AuthResponse("fake-jwt-token", DateTime.UtcNow.AddHours(1)));

        var publisherMock = new Mock<IEventPublisher>();
        var dbContext = CreateInMemoryContext();

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            dbContext,
            tokenServiceMock.Object,
            publisherMock.Object,
            NullLogger<RegisterUserHandler>.Instance);

        var command = new RegisterUserCommand(
            new RegisterRequest("test@example.com", "Password123!", "Test User"));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("fake-jwt-token");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserProfile()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        var userId = Guid.NewGuid().ToString();

        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => user.Id = userId);

        var tokenServiceMock = new Mock<IJwtTokenService>();
        tokenServiceMock
            .Setup(t => t.CreateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns(new AuthResponse("token", DateTime.UtcNow.AddHours(1)));

        var dbContext = CreateInMemoryContext();

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            dbContext,
            tokenServiceMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<RegisterUserHandler>.Instance);

        var command = new RegisterUserCommand(
            new RegisterRequest("user@example.com", "Password123!", "John Doe"));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var profile = await dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        profile.Should().NotBeNull();
        profile!.DisplayName.Should().Be("John Doe");
        profile.Role.Should().Be("User");
    }

    [Fact]
    public async Task Handle_WhenUserManagerFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email already taken" }));

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            CreateInMemoryContext(),
            new Mock<IJwtTokenService>().Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<RegisterUserHandler>.Instance);

        var command = new RegisterUserCommand(
            new RegisterRequest("taken@example.com", "Password123!", "User"));

        // Act
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email already taken*");
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesUsersCreatedEvent()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, _) => user.Id = Guid.NewGuid().ToString());

        var tokenServiceMock = new Mock<IJwtTokenService>();
        tokenServiceMock
            .Setup(t => t.CreateToken(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Returns(new AuthResponse("token", DateTime.UtcNow.AddHours(1)));

        var publisherMock = new Mock<IEventPublisher>();

        var handler = new RegisterUserHandler(
            userManagerMock.Object,
            CreateInMemoryContext(),
            tokenServiceMock.Object,
            publisherMock.Object,
            NullLogger<RegisterUserHandler>.Instance);

        var command = new RegisterUserCommand(
            new RegisterRequest("pub@example.com", "Password123!", "Publisher"));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("users.created");
    }
}
