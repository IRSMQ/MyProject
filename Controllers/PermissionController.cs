using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Service;
using Test26.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace Test26.Controller;

[Route("api/[controller]/[action]")]
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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Permissions
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new PermissionDto
            {
                PermissionId = e.PermissionId,
                PermissionName = e.PermissionName,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PermissionDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.Permissions
            .AsNoTracking()
            .Where(e => e.PermissionId == id && !e.IsDeleted)
            .Select(e => new PermissionDto
            {
                PermissionId = e.PermissionId,
                PermissionName = e.PermissionName,
            })
            .FirstOrDefaultAsync();

        if (entity == null)
            throw new KeyNotFoundException($"Permission Not Found");

        return Ok(ApiResponse<PermissionDto>.Success(entity,$"Get All Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] PermissionAddDto dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(e => e.PermissionName == dto.PermissionName && !e.IsDeleted);

        string text;

        if (entity != null)
            throw new ArgumentException("Permission already exists.");

        if (dto.PermissionId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditPermission");

            entity = await _context.Permissions
                .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Permission Not Found");

            if (entity.PermissionName == dto.PermissionName)
            {
                text = "Permission Exist";
            }
            else
            {
                var logId = await _logService.Log("Edit", nameof(Permission), entity.PermissionId);

                await _logService.ChangLog(
                    logId,
                    entity.PermissionName.ToString(),
                    dto.PermissionName.ToString(),
                    nameof(entity.PermissionName)
                );

                entity.PermissionName = dto.PermissionName;
                text = "Edit Succeeded";
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddPermission");
            entity = new Permission
            {
                PermissionId = Guid.NewGuid(),
                PermissionName = dto.PermissionName,
                IsDeleted = false
            };

            await _context.Permissions.AddAsync(entity);

            var logId = await _logService.Log("Create", nameof(Permission), entity.PermissionId);

            await _logService.ChangLog(
                logId,
                "null",
                entity.PermissionName,
                nameof(entity.PermissionName)
            );

            text = "Add Succeeded";
        }

        var newEntityDto = new PermissionDto
        {
            PermissionId = entity.PermissionId,
            PermissionName = entity.PermissionName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PermissionDto>.Success(newEntityDto, $"{text}"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeletePermission");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Permissions
            .FirstOrDefaultAsync(e => e.PermissionId == id && !e.IsDeleted)
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