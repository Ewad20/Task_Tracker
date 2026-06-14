using AutoMapper;
using NotificationService.Contracts.Notifications;
using NotificationService.Entities;

namespace NotificationService.Mapping;

public sealed class NotificationMapping : Profile
{
    public NotificationMapping()
    {
        CreateMap<Notification, NotificationDto>();
    }
}
