using AutoMapper;
using MediatR;
using ReportingService.Contracts.Reports;
using ReportingService.Entities;
using ReportingService.Repositories;

namespace ReportingService.Features.Reports;

public sealed record UpsertProjectReportCommand(UpsertProjectReportRequest Request) : IRequest<ProjectReportDto>;

public sealed class UpsertProjectReportHandler(IReportRepository repository, IMapper mapper)
    : IRequestHandler<UpsertProjectReportCommand, ProjectReportDto>
{
    public async Task<ProjectReportDto> Handle(UpsertProjectReportCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByProjectIdAsync(request.Request.ProjectId, cancellationToken);
        var progress = request.Request.TotalTasks == 0
            ? 0
            : Math.Round((double)request.Request.CompletedTasks / request.Request.TotalTasks * 100, 2);

        if (existing is null)
        {
            var report = new ProjectReport
            {
                Id = Guid.NewGuid(),
                ProjectId = request.Request.ProjectId,
                TotalTasks = request.Request.TotalTasks,
                CompletedTasks = request.Request.CompletedTasks,
                ProgressPercent = progress,
                UpdatedAt = DateTime.UtcNow
            };

            await repository.AddAsync(report, cancellationToken);
            return mapper.Map<ProjectReportDto>(report);
        }

        existing.TotalTasks = request.Request.TotalTasks;
        existing.CompletedTasks = request.Request.CompletedTasks;
        existing.ProgressPercent = progress;
        existing.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(existing, cancellationToken);
        return mapper.Map<ProjectReportDto>(existing);
    }
}
