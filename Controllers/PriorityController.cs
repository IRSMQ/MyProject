using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;
using Test26.Models;
using Test26.Context;

namespace Test26.Controller;

[Route("api/[controller]")]
[ApiController]
public class PriorityController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    public PriorityController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Priorities
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PriorityDto
            {
                PriorityId = p.PriorityId,
                PriorityName = p.PriorityName,
            })
            .ToListAsync();



        return Ok(ApiResponse<List<PriorityDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("getByID/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.Priorities
            .AsNoTracking()
            .Where(p => p.PriorityId == id && !p.IsDeleted)
            .Select(p => new PriorityDto
            {
                PriorityId = p.PriorityId,
                PriorityName = p.PriorityName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Priority with ID {id} not found.");

        return Ok(ApiResponse<PriorityDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] PriorityAddDto dto)
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
        
        var enrty = _context.Entry(entity);

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

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PriorityDto>.Success(PriorityToDto(entity),$"Add Succeeded"));
    }

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] PriorityDto dto)
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
                ColumnName = p.Metadata.Name,
                OldValue = p.OriginalValue?.ToString() ?? "null",
                NewValue = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();
        

        if (modifiedProperties.Any())
        {
            var logId = await _logService.Log("Edit", nameof(Priority), entity.PriorityId);

            foreach (var prop in modifiedProperties)
                await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.ColumnName);
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PriorityDto>.Success(PriorityToDto(entity),$"Edit Succeeded"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Priorities
            .FirstOrDefaultAsync(p => p.PriorityId == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"Priority with ID {id} not found.");

        entity.IsDeleted = true;

        var logId = await _logService.Log("Delete", nameof(Priority), entity.PriorityId);

        await _logService.ChangLog(logId, "false", "true", nameof(entity.IsDeleted));

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PriorityDto>.Success(PriorityToDto(entity),$"Delete Succeeded"));
    }


    private static PriorityDto PriorityToDto(Priority priority)
    {
        return new PriorityDto
        {
            PriorityId = priority.PriorityId,
            PriorityName = priority.PriorityName
        };
    }
}