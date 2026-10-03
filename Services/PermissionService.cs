using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Test26.Data;
using Test26.DTOs;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;

public interface IPermissionService
{
    //public Task<PermissionDto> Add(PermissionAddDto permissionAddDto);
}

public class PermissionService : PublicService<Permission,PermissionDto,PermissionAddDto>, IPermissionService
{
    public PermissionService(ProjectManagementSystemContext context, EventService eventService)
    : base(context, eventService) { }

    protected override Guid GetId(Permission entity) => entity.PermissionId;
    protected override Guid GetDtoId(PermissionDto d) => d.PermissionId;
    protected override string GetEntityName() => "Permission";
    protected override Expression<Func<Permission, PermissionDto>> ToDto => entity => new PermissionDto
    {
        PermissionId = entity.PermissionId,
        PermissionName = entity.PermissionName
    };
    protected override Permission ToEntity(PermissionAddDto d) => new()
    {
        PermissionName = d.PermissionName
    };
    protected override void UpdateEntity(Permission entity, PermissionDto d)
        => entity.PermissionName = d.PermissionName;
    protected override IQueryable<Permission> ApplyDuplicateCheck(IQueryable<Permission> q, PermissionAddDto d)
        => q.Where(p => p.PermissionName == d.PermissionName);


    protected override Guid RelatedTableID => Guid.Parse("6AE201D3-F58C-4D92-B135-E1D3A2116B87");
    protected override Guid GetEntityID(Permission entity) => entity.PermissionId;

}