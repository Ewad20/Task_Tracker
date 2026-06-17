using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProjectService.Contracts.Projects;
using ProjectService.Entities;
using ProjectService.Features.Projects;
using ProjectService.Messaging;
using ProjectService.Repositories;

namespace ProjectService.Tests.Features;

public class CreateProjectHandlerTests
{
    private static ProjectDto MakeDto(Project p) =>
        new(p.Id, p.Name, p.Description ?? "", p.OwnerId, p.CreatedAt,
            p.Members.Select(m => new ProjectMemberDto(m.Id, m.UserId, m.Role)).ToArray());

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProjectDto()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateProjectHandler>.Instance,
            mapperMock.Object);

        // NOTE: OwnerId is the FIRST parameter in CreateProjectCommand
        var command = new CreateProjectCommand(
            "owner-1",
            new CreateProjectRequest("Test Project", "Description", null));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Project");
        result.OwnerId.Should().Be("owner-1");
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesProjectCreatedEvent()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var publisherMock = new Mock<IEventPublisher>();
        var mapperMock = new Mock<IMapper>();

        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            publisherMock.Object,
            NullLogger<CreateProjectHandler>.Instance,
            mapperMock.Object);

        var command = new CreateProjectCommand(
            "owner-1",
            new CreateProjectRequest("New Project", "", null));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var publishCalls = publisherMock.Invocations
            .Where(i => i.Method.Name == "Publish")
            .Select(i => (string)i.Arguments[0])
            .ToList();

        publishCalls.Should().Contain("projects.created");
    }

    [Fact]
    public async Task Handle_CommandWithMembers_AddsInitialMembers()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        Project? capturedProject = null;

        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<CreateProjectHandler>.Instance,
            mapperMock.Object);

        var memberIds = new[] { "user-1", "user-2" };
        var command = new CreateProjectCommand(
            "owner-1",
            new CreateProjectRequest("Team Project", "", memberIds));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProject.Should().NotBeNull();
        capturedProject!.Members.Should().HaveCount(2);
        capturedProject.Members.Select(m => m.UserId).Should().BeEquivalentTo(memberIds);
        capturedProject.Members.Should().AllSatisfy(m => m.Role.Should().Be("Member"));
    }

    [Fact]
    public async Task Handle_DuplicateMemberIds_DeduplicatesMembers()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        Project? capturedProject = null;

        repoMock.Setup(r => r.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, _) => capturedProject = p)
            .Returns(Task.CompletedTask);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new CreateProjectHandler(
            repoMock.Object,
            new Mock<IEventPublisher>().Object,
            NullLogger<CreateProjectHandler>.Instance,
            mapperMock.Object);

        // user-1 appears twice
        var memberIds = new[] { "user-1", "user-1", "user-2" };
        var command = new CreateProjectCommand(
            "owner-1",
            new CreateProjectRequest("Project", "", memberIds));

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedProject!.Members.Select(m => m.UserId).Should().HaveCount(2,
            "duplicate user IDs should be deduplicated");
    }
}
