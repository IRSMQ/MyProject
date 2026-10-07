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


    protected abstract Guid RelatedTableID { get; }
    protected abstract Guid GetEntityID(TEntity entity);


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
            ??
            throw new KeyNotFoundException($"{GetEntityName()} WIth ID: {GetDtoId(dto)} Not Exist");
    

        UpdateEntity(entity,dto);
        await _projectManagementSystemContext.SaveChangesAsync();

        var tablename = _projectManagementSystemContext.Model
            .FindEntityType(typeof(TEntity))?
            .GetTableName() ?? typeof(TEntity).Name;

        await _logService.Log(
            logTypeName: "Edit",
            tableName: tablename,
            relatedId: GetEntityID(entity)
        );

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
            relatedId: GetEntityID(entity)
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

        await _logService.Log(
            logTypeName: "Create",
            tableName: tablename,
            relatedId: GetEntityID(entity)
        );

        return ToDto.Compile()(entity);
    }
}