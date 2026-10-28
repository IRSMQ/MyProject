using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class RolePermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public RolePermissionController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new RPDto
            {
                Rpid = e.RolePermissionId,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new ArgumentException("There are no RolePermissions");

        return Ok(ApiResponse<List<RPDto>>.Success(entity, $"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .Select(e => new RPDto
            {
                Rpid = e.RolePermissionId,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"RolePermission Not Found");

        return Ok(ApiResponse<RPDto>.Success(entity, $"Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetByRoleId([FromQuery] Guid id)
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .Select(e => new RPDto
            {
                Rpid = e.RolePermissionId,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException($"RolePermission Not Found");

        return Ok(ApiResponse<List<RPDto>>.Success(entity, $"Succeeded"));
    }


    [HttpGet]
    public async Task<IActionResult> GetByPermissionId([FromQuery] Guid id)
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.PermissionId == id && !e.IsDeleted)
            .Select(e => new RPDto
            {
                Rpid = e.RolePermissionId,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException($"RolePermission Not Found");

        return Ok(ApiResponse<List<RPDto>>.Success(entity, $"Succeeded"));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] RPDtoAdd dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.RolePermissions
            .Include(e => e.Role)
            .Include(e => e.Permission)
            .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && e.PermissionId == dto.PermissionId && !e.IsDeleted);

            if (entity != null)
                throw new ArgumentException($"This permission not available for this Role");

        string text;

        if (dto.RPID.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditRolePermission");
            var entityRole = await _context.Roles
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Role Not Found");

            var entityPermission = await _context.Permissions
                .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Permission Not Found");

            if (entity.RoleId == dto.RoleId && entity.PermissionId == dto.PermissionId)
            {
                text = "Role Permission Exist";
            }
            else
            {
                var logId = await _logService.Log("Edit", nameof(RolePermission), entity.RolePermissionId);

                await _logService.ChangLog(
                    logId,
                    entity.RoleId.ToString(),
                    dto.RoleId.ToString(),
                    nameof(entity.RoleId)
                );

                await _logService.ChangLog(
                    logId,
                    entity.PermissionId.ToString(),
                    dto.PermissionId.ToString(),
                    nameof(entity.PermissionId)
                );

                entity.RoleId = dto.RoleId;
                entity.PermissionId = dto.PermissionId;

                text = "Edit Succeeded";
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddRolePermission");
            var RoleEntity = await _context.Roles
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Role Not Found");
            
            var PermissionEntity = await _context.Permissions
                .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Permission Not Found");

            var exists = await _context.RolePermissions
                .AnyAsync(e => e.RoleId == dto.RoleId && e.PermissionId == dto.PermissionId && e.IsDeleted == false);

            if (exists)
                throw new ArgumentException($"This Permission is available for this Role");

            entity = new RolePermission
            {
                RolePermissionId = Guid.NewGuid(),
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            };

            await _context.RolePermissions.AddAsync(entity);

            var logId = await _logService.Log("Create", nameof(RolePermission), entity.RolePermissionId);

            await _logService.ChangLog(
                logId,
                "null",
                dto.RoleId.ToString(),
                nameof(entity.RoleId)
            );

            await _logService.ChangLog(
                logId,
                "null",
                dto.PermissionId.ToString(),
                nameof(entity.PermissionId)
            );

            text = "Add Succeeded";
        }

        var newEntityDto = new RPDto
        {
            Rpid = entity.RolePermissionId,
            RoleID = entity.RoleId,
            PermissionID = entity.PermissionId,
            RoleName = entity.Role.RoleName,
            PermissionName = entity.Permission.PermissionName
        };
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(newEntityDto, $"{text}"));
    }


    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteRolePermission");

        //////////////////////////////////////////////////////
        
        var entity = await _context.RolePermissions
            .FirstOrDefaultAsync(e => e.RolePermissionId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Role Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(RolePermission), entity.RolePermissionId);

        var result = new RPDto
        {
            Rpid = entity.RolePermissionId,
            RoleID = entity.RoleId,
            PermissionID = entity.PermissionId,
            RoleName = entity.Role.RoleName,
            PermissionName = entity.Permission.PermissionName
        };
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(result,$"Delete Succeeded"));
    }
}
