using AutoMapper;
using MediatR;
using ReportingService.Contracts.Reports;
using ReportingService.Repositories;

namespace ReportingService.Features.Reports;

public sealed record GetProjectReportQuery(Guid ProjectId) : IRequest<ProjectReportDto>;

public sealed class GetProjectReportHandler(IReportRepository repository, IMapper mapper)
    : IRequestHandler<GetProjectReportQuery, ProjectReportDto>
{
    public async Task<ProjectReportDto> Handle(GetProjectReportQuery request, CancellationToken cancellationToken)
    {
        var report = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Report not found");

        return mapper.Map<ProjectReportDto>(report);
    }
}
