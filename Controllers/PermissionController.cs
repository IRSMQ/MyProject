using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Service;
using Test26.EventS;
using Test26.Models;
namespace Test26.Controller;

[Route("api/[controller]")]
[ApiController]
public class PermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    public PermissionController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Permissions
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PermissionDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.Permissions
            .AsNoTracking()
            .Where(p => p.PermissionId == id && !p.IsDeleted)
            .Select(p => new PermissionDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
            })
            .FirstOrDefaultAsync();

        if (entity == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found.");

        return Ok(ApiResponse<PermissionDto>.Success(entity,$"Get All Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] PermissionAddDto dto)
    {
        var exists = await _context.Permissions
            .AnyAsync(p => p.PermissionName == dto.PermissionName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Permission already exists.");

        var eid = Guid.NewGuid();

        var entity = new Permission
        {
            PermissionId = eid,
            PermissionName = dto.PermissionName,
            IsDeleted = false
        };

        _context.Permissions.Add(entity);

        var logId = await _logService.Log("Create", nameof(Permission), eid);

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

        var newEntity = new PermissionDto
        {
            PermissionId = eid,
            PermissionName = dto.PermissionName
        };

        return Ok(ApiResponse<PermissionDto>.Success(newEntity,$"Add Succeeded"));
    }

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] PermissionDto dto)
    {
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == dto.PermissionId && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Permission with ID {dto.PermissionId} not found.");

        entity.PermissionName = dto.PermissionName;

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
            var logId = await _logService.Log("Edit", nameof(Permission), entity.PermissionId);

            foreach (var prop in modifiedProperties)
            {
                await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.Name);
            }
        }

        await _context.SaveChangesAsync();

        var newEntity = new PermissionDto
        {
            PermissionId = dto.PermissionId,
            PermissionName = dto.PermissionName
        };

        return Ok(ApiResponse<PermissionDto>.Success(newEntity,$"Edit Succeeded"));
    }

    

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Permission with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Permission), entity.PermissionId);

        var newEntity = new PermissionDto
        {
            PermissionId = id,
            PermissionName = entity.PermissionName
        };

        return Ok(ApiResponse<PermissionDto>.Success(newEntity,$"Delete Succeeded"));
    }
}