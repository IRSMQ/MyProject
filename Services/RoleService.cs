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

public class RoleService : RoleInterface
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    public RoleService(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<List<RoleDto>> GetAll()
    {
        return await _context.Roles
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new RoleDto
            {
                RoleId = p.RoleId,
                RoleName = p.RoleName,
            })
            .ToListAsync();
    }

    public async Task<RoleDto> GetById(Guid id)
    {
        var entity = await _context.Roles
            .AsNoTracking()
            .Where(p => p.RoleId == id && !p.IsDeleted)
            .Select(p => new RoleDto
            {
                RoleId = p.RoleId,
                RoleName = p.RoleName
            })
            .FirstOrDefaultAsync();

        if (entity == null)
            throw new KeyNotFoundException($"Role with ID {id} not found.");

        return entity;
    }

    public async Task<RoleDto> Add(RoleAddDto dto)
    {
        var exists = await _context.Roles
            .AnyAsync(p => p.RoleName == dto.RoleName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Role already exists.");

        

        var entity = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = dto.RoleName,
            IsDeleted = false
        };

        _context.Roles.Add(entity);
        await _context.SaveChangesAsync();

        var logId = await _logService.Log("Create", nameof(Role), entity.RoleId);

        var entry = _context.Entry(entity);
        foreach (var prop in entry.Properties)
        {
            await _logService.ChangLog(
                logId,
                oldValue: "null",
                newValue: prop.CurrentValue?.ToString() ?? "null",
                columnName: prop.Metadata.Name
            );
        }

        return new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };
    }

    public async Task<RoleDto> EditAsync(RoleDto dto)
    {
        var entity = await _context.Roles
            .FirstOrDefaultAsync(p => p.RoleId == dto.RoleId && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Role with ID {dto.RoleId} not found.");

        entity.RoleName = dto.RoleName;

        var entry = _context.Entry(entity);
        var modifiedProperties = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => new
            {
                Name = p.Metadata.Name,
                OldValue = p.OriginalValue?.ToString() ?? "null",
                NewValue = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        await _context.SaveChangesAsync();

        if (modifiedProperties.Any())
        {
            var logId = await _logService.Log("Edit", nameof(Role), entity.RoleId);

            foreach (var prop in modifiedProperties)
            {
                await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.Name);
            }
        }

        return new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };
    }

    public async Task<RoleDto> DeleteAsync(Guid id)
    {
        var entity = await _context.Roles
            .FirstOrDefaultAsync(p => p.RoleId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Role with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Role), entity.RoleId);

        return new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };
    }
}