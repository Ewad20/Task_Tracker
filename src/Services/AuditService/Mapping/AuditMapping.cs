using AutoMapper;
using AuditService.Contracts.Audit;
using AuditService.Entities;

namespace AuditService.Mapping;

public sealed class AuditMapping : Profile
{
    public AuditMapping()
    {
        CreateMap<AuditLog, AuditLogDto>();
    }
}
