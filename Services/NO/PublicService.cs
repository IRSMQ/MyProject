/*
using System.ClientModel.Primitives;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Test26.Context;
using Test26.EventS;

namespace Test26.Service;

public abstract class PublicService<TEntity, TDto, TAddDto>
    where TEntity : class
    where TDto : class
    where TAddDto : class
{
    protected readonly ProjectManagementSystemContext _projectManagementSystemContext;
    protected readonly LogService _logService;
    protected PublicService(ProjectManagementSystemContext projectManagementSystemContext, LogService logService)
    {
        _projectManagementSystemContext = projectManagementSystemContext;
        _logService = logService;
    }

    protected abstract Guid GetId(TEntity entity);
    protected abstract Guid GetDtoId(TDto dto);
    protected abstract string GetEntityName();
    protected abstract Expression<Func<TEntity, TDto>> ToDto { get; }
    protected abstract TEntity ToEntity(TAddDto addDto);
    protected abstract void UpdateEntity(TEntity entity, TDto dto);
    protected abstract IQueryable<TEntity> ApplyDuplicateCheck(IQueryable<TEntity> query, TAddDto addDto);

    protected abstract Expression<Func<TEntity, Guid>> MatchById { get; }


    public virtual async Task<List<TDto>> GetAll()
    {
        var list = await _projectManagementSystemContext
            .Set<TEntity>()
            .Where(e => !((ISoftDeletable)e).IsDeleted)
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
            .FirstOrDefaultAsync(e => GetId(e) == id && !((ISoftDeletable)e).IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"{GetEntityName()} With ID: {id} Not Exist");

        return ToDto.Compile()(entity);
    }
    public virtual async Task<TDto> Edit(TDto dto)
    {
        
        var entity = await _projectManagementSystemContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(e => GetId(e) == GetDtoId(dto) && !((ISoftDeletable)e).IsDeleted)
            ?? throw new KeyNotFoundException($"{GetEntityName()} WIth ID: {GetDtoId(dto)} Not Exist");
    

        UpdateEntity(entity,dto);

        
        var query = _projectManagementSystemContext
            .Set<TEntity>()
            .Where(MatchById(GetDtoId(dto)));

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
            query = query.Where(e => !((ISoftDeletable)e).IsDeleted);

        var entity = await query.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"{GetEntityName} with ID {GetDtoId(dto)} not found");

        

        await _projectManagementSystemContext.SaveChangesAsync();

        var entry = _projectManagementSystemContext.Entry(entity);

        var modifiedProps = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => new
            {
                ColumnName = p.Metadata.Name,
                OldVal = p.OriginalValue?.ToString() ?? "null",
                NewVal = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        if (modifiedProps.Any())
        {
            var tablename = _projectManagementSystemContext.Model
                .FindEntityType(typeof(TEntity))?
                .GetTableName() ?? typeof(TEntity).Name;

            var logId = await _logService.Log("Edit", tablename, GetId(entity));

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);

            await _projectManagementSystemContext.SaveChangesAsync();
        }

        return ToDto.Compile()(entity);
    }
    public virtual async Task<TDto> Delete(Guid id)
    {
        var entity = await _projectManagementSystemContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(e => GetId(e) == id && !((ISoftDeletable)e).IsDeleted)
        ?? throw new KeyNotFoundException($"{GetEntityName()} With ID: {id} Not Exist");


        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            _projectManagementSystemContext.Set<TEntity>().Update(entity);
        }
        else
        {
            _projectManagementSystemContext.Set<TEntity>().Remove(entity);
        }

        await _projectManagementSystemContext.SaveChangesAsync();

        var tablename = _projectManagementSystemContext.Model
            .FindEntityType(typeof(TEntity))?
            .GetTableName() ?? typeof(TEntity).Name;

        await _logService.Log(
            logTypeName: "Delete",
            tableName: tablename,
            relatedId: GetId(entity)
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

        var tablename = _projectManagementSystemContext.Model
            .FindEntityType(typeof(TEntity))?
            .GetTableName() ?? typeof(TEntity).Name;


        var logid = await _logService.Log(
            logTypeName: "Create",
            tableName: tablename,
            relatedId: GetId(entity)
        );

        var entry = _projectManagementSystemContext.Entry(entity);

        foreach(var e in entry.Properties)
            await _logService.ChangLog(logid, "null", e.CurrentValue?.ToString() ?? "null", e.Metadata.Name);


        await _projectManagementSystemContext.SaveChangesAsync();

        return ToDto.Compile()(entity);
    }
}
*/