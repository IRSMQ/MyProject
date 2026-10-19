using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Service;
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
        var exist = await _context.Permissions
            .AnyAsync(e => e.PermissionName == dto.PermissionName && !e.IsDeleted);

        var newEntity = new PermissionDto();
        string text;

        if (exist)
            throw new InvalidOperationException("Permission already exists.");

        else if (dto.PermissionId.HasValue)
        {
            var entity = await _context.Permissions
                .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId)
                ?? throw new KeyNotFoundException("Permission Not Found");

            

            var logId = await _logService.Log("Edit", nameof(Permission), entity.PermissionId);

            await _logService.ChangLog(
                logId,
                entity.PermissionName.ToString(),
                dto.PermissionName.ToString(),
                nameof(entity.PermissionName)
            );

            entity.PermissionName = dto.PermissionName;

            newEntity.PermissionId = entity.PermissionId;
            newEntity.PermissionName = entity.PermissionName;
            text = "Edit Succeeded";
        }
        else
        {
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
                if (prop.OriginalValue != null && prop.CurrentValue != null && prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        oldValue: "null",
                        newValue: prop.CurrentValue?.ToString() ?? "null",
                        columnName: prop.Metadata.Name
                    );
                }
            }

            newEntity.PermissionId = eid;
            newEntity.PermissionName = dto.PermissionName;
            text = "Add Succeeded";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PermissionDto>.Success(newEntity,$"{text}"));
    }


    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == id && !p.IsDeleted)
            ?? throw new KeyNotFoundException($"Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Permission), entity.PermissionId);


        /////////////////////////////////////////////////////////
        var entityRolePermission = await _context.RolePermissions
            .Where(e => e.PermissionId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityRolePermission)
        {
            e.IsDeleted = true;
            await _logService.Log("Delete", nameof(RolePermission), e.RolePermissionId);
        }


        /////////////////////////////////////////////////////////
        var entityUserPermission = await _context.UserPermissions
            .Where(e => e.PermissionId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityUserPermission)
        {
            e.IsDeleted = true;
            await _logService.Log("Delete", nameof(UserPermission), e.UserPermissionId);
        }

        var newEntity = new PermissionDto
        {
            PermissionId = id,
            PermissionName = entity.PermissionName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PermissionDto>.Success(newEntity, $"Delete Succeeded"));
    }
}