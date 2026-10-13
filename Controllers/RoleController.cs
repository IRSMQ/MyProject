using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public RoleController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Roles
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new RoleDto
            {
                RoleId = p.RoleId,
                RoleName = p.RoleName,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<RoleDto>>.Success(entity,$"Succeeded"));
    }

    [HttpGet("getbyid/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.Roles
            .AsNoTracking()
            .Where(p => p.RoleId == id && !p.IsDeleted)
            .Select(p => new RoleDto
            {
                RoleId = p.RoleId,
                RoleName = p.RoleName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Role with ID {id} not found.");

        return Ok(ApiResponse<RoleDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] RoleDto dto)
    {
        var exists = await _context.Roles
            .AnyAsync(p => p.RoleName == dto.RoleName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Role already exists.");

        var role = new RoleDto { };

        if (dto.RoleId.HasValue)
        {
            var entity = await _context.Roles
                .FindAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("Role with this ID not found");

            entity.RoleName = dto.RoleName;

            var entry = _context.Entry(entity);

            var modifiedProperties = entry.Properties
                .Where(e => e.IsModified)
                .Select(e => new
                {
                    CulomnName = e.Metadata.Name,
                    OldValue = e.OriginalValue?.ToString() ?? "null",
                    NewValue = e.CurrentValue?.ToString() ?? "null"
                })
                .ToList();

            if (modifiedProperties.Any())
            {
                var logId = await _logService.Log("Edit", nameof(Role), entity.RoleId);

                foreach (var prop in modifiedProperties)
                    await _logService.ChangLog(logId, prop.OldValue, prop.NewValue, prop.CulomnName);
            }

            role.RoleId = entity.RoleId;
            role.RoleName = entity.RoleName;
        }

        else
        {
            var newEntity = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = dto.RoleName,
                IsDeleted = false
            };

            _context.Roles.Add(newEntity);

            var logId = await _logService.Log("Create", nameof(Role), newEntity.RoleId);

            var entry = _context.Entry(newEntity);

            foreach (var prop in entry.Properties)
            {
                if (prop.OriginalValue != null && prop.CurrentValue != null && prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name
                    );
                }
            }
            role.RoleId = newEntity.RoleId;
            role.RoleName = newEntity.RoleName;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RoleDto>.Success(role,$"Succeeded"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Role with ID {id} not found.");

        

        var entityUser = await _context.Users
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var u in entityUser)
        {
            u.RoleId = null;

            var userLogId = await _logService.Log("Edit", nameof(User), u.UserId);

            await _logService.ChangLog(userLogId, id.ToString(), "null", nameof(u.RoleId));
        }

        var entityRolePermission = await _context.RolePermissions
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityRolePermission)
        {
            e.IsDeleted = true;
            var userLogId = await _logService.Log("Delete", nameof(RolePermission), e.Rpid);
        }

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Role), entity.RoleId);

        var newRole = new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RoleDto>.Success(newRole,$""));
    }
}
