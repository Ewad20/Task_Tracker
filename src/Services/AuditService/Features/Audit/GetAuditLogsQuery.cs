using AutoMapper;
using MediatR;
using AuditService.Contracts.Audit;
using AuditService.Repositories;

namespace AuditService.Features.Audit;

public sealed record GetAuditLogsQuery(AuditFilterRequest Filter) : IRequest<IReadOnlyList<AuditLogDto>>;

public sealed class GetAuditLogsHandler(IAuditRepository repository, IMapper mapper)
    : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogDto>>
{
    public async Task<IReadOnlyList<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var filter = new AuditFilter
        {
            UserId = request.Filter.UserId,
            Action = request.Filter.Action,
            EntityType = request.Filter.EntityType
        };

        var logs = await repository.GetFilteredAsync(filter, cancellationToken);
        return logs.Select(mapper.Map<AuditLogDto>).ToList();
    }
}
