using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;

public interface StatusInterface
{
    //Task<StatusDto> Add(StatusAddDto statusDto);
}

public class StatusService : PublicService<Status,StatusDto,StatusAddDto>, StatusInterface
{
    public StatusService(ProjectManagementSystemContext context,LogService eventService)
    : base(context,eventService) { }

    protected override Guid GetId(Status entity) => entity.StatusId;
    protected override Guid GetDtoId(StatusDto dto) => dto.StatusId;
    protected override string GetEntityName() => "Status";
    protected override Expression<Func<Status, StatusDto>> ToDto => entity => new StatusDto
    {
        StatusId = entity.StatusId,
        StatusName = entity.StatusName
    };
    protected override Status ToEntity(StatusAddDto addDto) => new()
    {
        StatusName = addDto.StatusName
    };
    protected override void UpdateEntity(Status entity, StatusDto dto)
        =>  entity.StatusName = dto.StatusName;
    protected override IQueryable<Status> ApplyDuplicateCheck(IQueryable<Status> query, StatusAddDto addDto)
        =>  query.Where(p => p.StatusName == addDto.StatusName);



    protected override Guid RelatedTableID => Guid.Parse("FE25E542-9559-47C3-809B-584C2CE1860A");
    protected override Guid GetEntityID(Status entity) => entity.StatusId;


}