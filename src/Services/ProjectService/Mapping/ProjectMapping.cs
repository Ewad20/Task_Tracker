using AutoMapper;
using ProjectService.Contracts.Projects;
using ProjectService.Entities;

namespace ProjectService.Mapping;

public sealed class ProjectMapping : Profile
{
    public ProjectMapping()
    {
        CreateMap<Project, ProjectDto>();
        CreateMap<ProjectMember, ProjectMemberDto>();
    }
}
