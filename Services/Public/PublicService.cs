using System.ClientModel.Primitives;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Test26.Data;
using Test26.EventS;

namespace Test26.Service;

public abstract class PublicService<TEntity, TDto, TAddDto>
    where TEntity : class
    where TDto : class
    where TAddDto : class
{
    protected readonly ProjectManagementSystemContext _projectManagementSystemContext;
    protected readonly EventService _eventService;
    protected PublicService(ProjectManagementSystemContext projectManagementSystemContext, EventService eventService)
    {
        _projectManagementSystemContext = projectManagementSystemContext;
        _eventService = eventService;
    }

    protected abstract Guid GetId(TEntity entity);
    protected abstract Guid GetDtoId(TDto dto);
    protected abstract string GetEntityName();
    protected abstract Expression<Func<TEntity, TDto>> ToDto { get; }
    protected abstract TEntity ToEntity(TAddDto addDto);
    protected abstract void UpdateEntity(TEntity entity, TDto dto);
    protected abstract IQueryable<TEntity> ApplyDuplicateCheck(IQueryable<TEntity> query, TAddDto addDto);


    protected abstract Guid RelatedTableID { get; }
    protected abstract Guid GetEntityID(TEntity entity);


    public virtual async Task<List<TDto>> GetAll()
    {
        var list = await _projectManagementSystemContext
            .Set<TEntity>()
            .Select(ToDto)
            .ToListAsync();

        if (list.Count == 0)
            throw new InvalidOperationException($"There are no {GetEntityName()}s");

        return list;
    }
    public virtual async Task<TDto> GetById(Guid id)
    {
        var entity = await _projectManagementSystemContext
            .Set<TEntity>()
            .FindAsync(id);

        if (entity == null)
            throw new KeyNotFoundException($"{GetEntityName()} With ID: {id} Not Exist");

        return ToDto.Compile()(entity);
    }
    public virtual async Task<TDto> Edit(TDto dto)
    {
        var entity = await _projectManagementSystemContext
            .Set<TEntity>()
            .FindAsync(GetDtoId(dto))
            ??
            throw new KeyNotFoundException($"{GetEntityName()} WIth ID: {GetDtoId(dto)} Not Exist");
    

        UpdateEntity(entity,dto);
        await _projectManagementSystemContext.SaveChangesAsync();

        await _eventService.Log(
            eventTypeId: Guid.Parse("4C13B6AD-66E9-4532-A0C3-76E7C2BD29E9"),
            tableId: RelatedTableID,
            relatedId: GetEntityID(entity),
            description: $"Edit"
        );

        return ToDto.Compile()(entity);
    }
    public virtual async Task<TDto> Delete(Guid id)
    {
        var entity = await _projectManagementSystemContext
            .Set<TEntity>()
            .FindAsync(id)
            ??
            throw new KeyNotFoundException($"{GetEntityName()} With ID: {id} Not Exist");

        _projectManagementSystemContext.Set<TEntity>().Remove(entity);
        await _projectManagementSystemContext.SaveChangesAsync();

        await _eventService.Log(
            eventTypeId: Guid.Parse("016620E4-1B1E-45F1-9AFD-362FB47CE0FD"),
            tableId: RelatedTableID,
            relatedId: GetEntityID(entity),
            description: $"Delete with ID: {id}"
        );

        return ToDto.Compile()(entity);
    }
    public virtual async Task<TDto> Add(TAddDto addDto)
    {
        var query = _projectManagementSystemContext
            .Set<TEntity>()
            .AsQueryable();
        
        query = ApplyDuplicateCheck(query,addDto);

        if (await query.AnyAsync())
            throw new InvalidOperationException($"{GetEntityName()} already exists");

        var entity = ToEntity(addDto);
        _projectManagementSystemContext.Set<TEntity>().Add(entity);
        
        await _projectManagementSystemContext.SaveChangesAsync();

        await _eventService.Log(
            eventTypeId: Guid.Parse("B7BFF458-FBA0-4549-A7B2-0D3A2839B47C"),
            tableId: RelatedTableID,
            relatedId: GetEntityID(entity),
            description: $"Add"
        );

        return ToDto.Compile()(entity);
    }
}