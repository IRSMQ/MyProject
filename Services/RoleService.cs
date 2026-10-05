using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Test26.Context;
using Test26.DTOs;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;


public interface RoleInterface
{
    //public Task<RoleDto> Add(RoleAddDto roleDto);
}

public class RoleService :PublicService<Role,RoleDto,RoleAddDto>, RoleInterface
{
    public RoleService(ProjectManagementSystemContext context, LogService eventService)
    : base(context,eventService) { }

    protected override Guid GetId(Role entity) => entity.RoleId;
    protected override Guid GetDtoId(RoleDto dto) => dto.RoleId;
    protected override string GetEntityName() => "Role";
    protected override Expression<Func<Role, RoleDto>> ToDto => entity => new RoleDto
    {
        RoleId = entity.RoleId,
        RoleName = entity.RoleName
    };
    protected override Role ToEntity(RoleAddDto addDto) => new()
    {
        RoleName = addDto.RoleName
    };
    protected override void UpdateEntity(Role entity, RoleDto dto)
        => entity.RoleName = dto.RoleName;
    protected override IQueryable<Role> ApplyDuplicateCheck(IQueryable<Role> query, RoleAddDto addDto)
        => query.Where(p=> p.RoleName == addDto.RoleName);



    protected override Guid RelatedTableID => Guid.Parse("C528DB5C-2590-42CA-B4CF-20C8540DC33D");
    protected override Guid GetEntityID(Role entity) => entity.RoleId;

}