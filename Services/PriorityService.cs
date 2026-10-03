using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Test26.Data;
using Test26.DTOs;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;


public interface IPriorityService
{
    //public Task<PriorityDto> Add(PriorityAddDto priorityAddDto);
}

public class PriorityService : PublicService<Priority,PriorityDto,PriorityAddDto>, IPriorityService
{
    public PriorityService(ProjectManagementSystemContext context, EventService eventService)
    : base(context, eventService) { }
    protected override Guid GetId(Priority p) => p.PriorityId;
    protected override Guid GetDtoId(PriorityDto dto) => dto.PriorityId;
    protected override string GetEntityName() => "Priority";
    protected override Expression<Func<Priority, PriorityDto>> ToDto => entity => new PriorityDto
    {
        PriorityId = entity.PriorityId,
        PriorityName = entity.PriorityName
    };
    protected override Priority ToEntity(PriorityAddDto addDto) => new()
    {
        PriorityName = addDto.PriorityName
    };
    protected override void UpdateEntity(Priority entity, PriorityDto dto) 
        => entity.PriorityName = dto.PriorityName;
    protected override IQueryable<Priority> ApplyDuplicateCheck(IQueryable<Priority> query , PriorityAddDto addDto)
        => query.Where(p => p.PriorityName == addDto.PriorityName);
    protected override Guid RelatedTableID => Guid.Parse("73FA5D3A-291D-4043-9120-1FFF6F8C1843");
    protected override Guid GetEntityID(Priority entity) => entity.PriorityId;


}