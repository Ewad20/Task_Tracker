using AutoMapper;
using ReportingService.Contracts.Reports;
using ReportingService.Entities;

namespace ReportingService.Mapping;

public sealed class ReportMapping : Profile
{
    public ReportMapping()
    {
        CreateMap<ProjectReport, ProjectReportDto>();
    }
}
