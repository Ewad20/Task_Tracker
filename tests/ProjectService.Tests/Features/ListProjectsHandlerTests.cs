using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectService.Contracts.Projects;
using ProjectService.Entities;
using ProjectService.Features.Projects;
using ProjectService.Repositories;

namespace ProjectService.Tests.Features;

public class ListProjectsHandlerTests
{
    private static ProjectDto MakeDto(Project p) =>
        new(p.Id, p.Name, p.Description ?? "", p.OwnerId, p.CreatedAt,
            p.Members.Select(m => new ProjectMemberDto(m.Id, m.UserId, m.Role)).ToArray());

    [Fact]
    public async Task Handle_AdminUser_ReturnsAllProjects()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var mapperMock = new Mock<IMapper>();

        var projects = new List<Project>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", OwnerId = "owner-1", Members = new List<ProjectMember>() },
            new() { Id = Guid.NewGuid(), Name = "Beta",  OwnerId = "owner-2", Members = new List<ProjectMember>() }
        };

        repoMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new ListProjectsHandler(repoMock.Object, mapperMock.Object);
        var query = new ListProjectsQuery("admin-user", true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().BeEquivalentTo(["Alpha", "Beta"]);
    }

    [Fact]
    public async Task Handle_RegularUser_ReturnsOnlyAccessibleProjects()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        var mapperMock = new Mock<IMapper>();
        var userId = "regular-user";

        var accessibleProjects = new List<Project>
        {
            new() { Id = Guid.NewGuid(), Name = "My Project", OwnerId = userId, Members = new List<ProjectMember>() }
        };

        repoMock.Setup(r => r.GetAllAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessibleProjects);
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => MakeDto(p));

        var handler = new ListProjectsHandler(repoMock.Object, mapperMock.Object);
        var query = new ListProjectsQuery(userId, false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().OwnerId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_EmptyProjectList_ReturnsEmptyCollection()
    {
        // Arrange
        var repoMock = new Mock<IProjectRepository>();
        repoMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<ProjectDto>(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectDto(p.Id, p.Name, "", p.OwnerId, p.CreatedAt, []));

        var handler = new ListProjectsHandler(repoMock.Object, mapperMock.Object);
        var query = new ListProjectsQuery("any-user", false);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
