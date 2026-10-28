using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]/[action]")]
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
            .Where(e => !e.IsDeleted)
            .Select(e => new RoleDto
            {
                RoleId = e.RoleId,
                RoleName = e.RoleName,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<RoleDto>>.Success(entity,$"Succeeded"));
    }

    [HttpGet("getbyid/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.Roles
            .AsNoTracking()
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .Select(e => new RoleDto
            {
                RoleId = e.RoleId,
                RoleName = e.RoleName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Role Not Found");

        return Ok(ApiResponse<RoleDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] RoleDto dto)
    {

        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleName == dto.RoleName && !e.IsDeleted);

        if (entity != null)
            throw new ArgumentException("Role already exists");

        string text;

        if (dto.RoleId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditRole");
            entity = await _context.Roles
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Role with this ID not found");

            if (entity.RoleName == dto.RoleName)
            {
                text = "Role Exist";
            }
            else
            {
                var logId = await _logService.Log("Edit", nameof(Role), dto.RoleId.Value);

                await _logService.ChangLog(
                    logId,
                    entity.RoleName,
                    dto.RoleName,
                    nameof(Role.RoleName)
                );

                entity.RoleName = dto.RoleName;
                text = "Edit Succeeded";
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddRole");
            entity = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = dto.RoleName,
                IsDeleted = false
            };

            await _context.Roles.AddAsync(entity);

            var logId = await _logService.Log("Create", nameof(Role), entity.RoleId);

            await _logService.ChangLog(
                logId,
                "null",
                entity.RoleId.ToString(),
                nameof(entity.RoleId)
                );

            await _logService.ChangLog(
                logId,
                "null",
                entity.RoleName,
                nameof(entity.RoleName)
                );


            await _context.AddAsync(entity);
            text = "Add Succeeded";
        }

        var newEntity = new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RoleDto>.Success(newEntity, $"{text}"));
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteRole");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Role Not Found");


        /////////////////////////////////////////////////////////
        var entityUser = await _context.Users
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var u in entityUser)
        {
            u.RoleId = null;

            var userLogId = await _logService.Log("Edit", nameof(User), u.UserId);

            await _logService.ChangLog(
                userLogId,
                id.ToString(),
                "null",
                nameof(u.RoleId)
            );
        }


        /////////////////////////////////////////////////////////
        var entityRolePermission = await _context.RolePermissions
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityRolePermission)
        {
            e.IsDeleted = true;
            var userLogId = await _logService.Log("Delete", nameof(RolePermission), e.RolePermissionId);
        }

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Role), entity.RoleId);

        var newRole = new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RoleDto>.Success(newRole, $"Delete Succeeded"));
    }
}
