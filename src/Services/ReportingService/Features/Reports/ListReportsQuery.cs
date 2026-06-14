using AutoMapper;
using MediatR;
using ReportingService.Contracts.Reports;
using ReportingService.Repositories;

namespace ReportingService.Features.Reports;

public sealed record ListReportsQuery() : IRequest<IReadOnlyList<ProjectReportDto>>;

public sealed class ListReportsHandler(IReportRepository repository, IMapper mapper)
    : IRequestHandler<ListReportsQuery, IReadOnlyList<ProjectReportDto>>
{
    public async Task<IReadOnlyList<ProjectReportDto>> Handle(ListReportsQuery request, CancellationToken cancellationToken)
    {
        var reports = await repository.GetAllAsync(cancellationToken);
        return reports.Select(mapper.Map<ProjectReportDto>).ToList();
    }
}
