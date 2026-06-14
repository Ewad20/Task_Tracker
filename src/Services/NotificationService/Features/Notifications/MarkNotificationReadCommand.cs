using AutoMapper;
using MediatR;
using NotificationService.Contracts.Notifications;
using NotificationService.Repositories;

namespace NotificationService.Features.Notifications;

public sealed record MarkNotificationReadCommand(Guid NotificationId, bool IsRead) : IRequest<NotificationDto>;

public sealed class MarkNotificationReadHandler(INotificationRepository repository, IMapper mapper)
    : IRequestHandler<MarkNotificationReadCommand, NotificationDto>
{
    public async Task<NotificationDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await repository.GetAsync(request.NotificationId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found");

        notification.IsRead = request.IsRead;
        await repository.UpdateAsync(notification, cancellationToken);

        return mapper.Map<NotificationDto>(notification);
    }
}
