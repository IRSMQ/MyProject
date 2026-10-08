using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public StatusController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<StatusDto>>> GetAll()
    {
        var result = await _context.Statuses
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new StatusDto
            {
                StatusId = p.StatusId,
                StatusName = p.StatusName,
            })
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("byid/{id:guid}")]
    public async Task<ActionResult<StatusDto>> GetById(Guid id)
    {
        var entity = await _context.Statuses
            .AsNoTracking()
            .Where(p => p.StatusId == id && !p.IsDeleted)
            .Select(p => new StatusDto
            {
                StatusId = p.StatusId,
                StatusName = p.StatusName
            })
            .FirstOrDefaultAsync();

        if (entity == null)
            throw new KeyNotFoundException($"Status with ID {id} not found.");

        return Ok(entity);
    }

    [HttpPost("add")]
    public async Task<ActionResult<StatusDto>> Add([FromBody] StatusAddDto dto)
    {
        var exists = await _context.Statuses
            .AnyAsync(p => p.StatusName == dto.StatusName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Status already exists.");

        var entity = new Status
        {
            StatusId = Guid.NewGuid(),
            StatusName = dto.StatusName,
            IsDeleted = false
        };

        _context.Statuses.Add(entity);
        await _context.SaveChangesAsync();

        var logId = await _logService.Log("Create", nameof(Status), entity.StatusId);

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

        return Ok(new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        });
    }

    [HttpPut("edit")]
    public async Task<ActionResult<StatusDto>> Edit([FromBody] StatusDto dto)
    {
        var entity = await _context.Statuses
            .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Status with ID {dto.StatusId} not found.");

        entity.StatusName = dto.StatusName;

        var entry = _context.Entry(entity);
        var modifiedProperties = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => new
            {
                ColumnName = p.Metadata.Name,
                OldValue = p.OriginalValue?.ToString() ?? "null",
                NewValue = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        await _context.SaveChangesAsync();

        if (modifiedProperties.Any())
        {
            var logId = await _logService.Log("Edit", nameof(Status), entity.StatusId);

            foreach (var prop in modifiedProperties)
            {
                await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.ColumnName);
            }
        }

        return Ok(new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        });
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<ActionResult<StatusDto>> Delete(Guid id)
    {
        var entity = await _context.Statuses
            .FirstOrDefaultAsync(p => p.StatusId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Status with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Status), entity.StatusId);

        return Ok(new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        });
    }
}
