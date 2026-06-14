using AutoMapper;
using MediatR;
using NotificationService.Contracts.Notifications;
using NotificationService.Repositories;

namespace NotificationService.Features.Notifications;

public sealed record GetNotificationsQuery(string UserId, Guid? ProjectId) : IRequest<IReadOnlyList<NotificationDto>>;

public sealed class GetNotificationsHandler(INotificationRepository repository, IMapper mapper)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var notifications = await repository.GetForUserAsync(request.UserId, request.ProjectId, cancellationToken);
        return notifications.Select(mapper.Map<NotificationDto>).ToList();
    }
}
