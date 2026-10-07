using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Test26.Context;
using Test26.DTOs;
using Test26.EventS;
using Test26.Models;

namespace Test26.Service;


public interface IPriorityService
{
    //public Task<PriorityDto> Add(PriorityAddDto priorityAddDto);
}

public class PriorityService : IPriorityService
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    public PriorityService(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<List<PriorityDto>> GetAll()
    {
        return await _context.Priorities
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PriorityDto
            {
                PriorityId = p.PriorityId,
                PriorityName = p.PriorityName,
            })
            .ToListAsync();
    }

    public async Task<PriorityDto> GetById(Guid id)
    {
        var priority = await _context.Priorities
            .AsNoTracking()
            .Where(p => p.PriorityId == id && !p.IsDeleted)
            .Select(p => new PriorityDto
            {
                PriorityId = p.PriorityId,
                PriorityName = p.PriorityName
            })
            .FirstOrDefaultAsync();

        if (priority == null)
            throw new KeyNotFoundException($"Priority with ID {id} not found.");

        return priority;
    }

    public async Task<PriorityDto> Add(PriorityAddDto dto)
    {
        var exists = await _context.Priorities
            .AnyAsync(p => p.PriorityName == dto.PriorityName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Priority already exists.");

        

        var entity = new Priority
        {
            PriorityId = Guid.NewGuid(),
            PriorityName = dto.PriorityName,
            IsDeleted = false
        };

        _context.Priorities.Add(entity);
        await _context.SaveChangesAsync();

        var logId = await _logService.Log("Create", nameof(Priority), entity.PriorityId);

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

        return new PriorityDto
        {
            PriorityId = entity.PriorityId,
            PriorityName = entity.PriorityName
        };
    }

    public async Task<PriorityDto> EditAsync(PriorityDto dto)
    {
        var entity = await _context.Priorities
            .FirstOrDefaultAsync(p => p.PriorityId == dto.PriorityId && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Priority with ID {dto.PriorityId} not found.");

        entity.PriorityName = dto.PriorityName;

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
            var logId = await _logService.Log("Edit", nameof(Priority), entity.PriorityId);

            foreach (var prop in modifiedProperties)
            {
                await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.Name);
            }
        }

        return new PriorityDto
        {
            PriorityId = entity.PriorityId,
            PriorityName = entity.PriorityName
        };
    }

    public async Task<PriorityDto> DeleteAsync(Guid id)
    {
        var entity = await _context.Priorities
            .FirstOrDefaultAsync(p => p.PriorityId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Priority with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Priority), entity.PriorityId);

        return new PriorityDto
        {
            PriorityId = entity.PriorityId,
            PriorityName = entity.PriorityName
        };
    }
}