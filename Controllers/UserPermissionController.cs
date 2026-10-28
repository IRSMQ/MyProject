using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Test26.Service;
using Test26.ApiR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserPermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public UserPermissionController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
            .Where(e => !e.IsDeleted)
            .Select(e => new UserPermissionDto
            {
                UserId = e.UserId,
                PermissionId = e.UserPermissionId,
                UserName = e.User.UserFullName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count() > 0)
            throw new KeyNotFoundException("User Permission Empity");

        return Ok(ApiResponse<List<UserPermissionDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("id/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
            .Where(e => e.UserPermissionId == id && !e.IsDeleted)
            .Select(e => new UserPermissionDto
            {
                UserId = e.UserId,
                PermissionId = e.UserPermissionId,
                UserName = e.User.UserFullName,
                PermissionName = e.Permission.PermissionName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User Permission Not Found");

        return Ok(ApiResponse<UserPermissionDto>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("userid/{id:guid}")]
    public async Task<IActionResult> GetByUserId(Guid id)
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
            .Where(e => e.UserId == id && !e.IsDeleted)
            .Select(e => new UserPermissionDto
            {
                UserId = e.UserId,
                PermissionId = e.UserPermissionId,
                UserName = e.User.UserFullName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("User Permission Not Found");

        return Ok(ApiResponse<List<UserPermissionDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("permissionid/{id:guid}")]
    public async Task<IActionResult> GetByPermissionId(Guid id)
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
            .Where(e => e.PermissionId == id && !e.IsDeleted)
            .Select(e => new UserPermissionDto
            {
                UserId = e.UserId,
                PermissionId = e.UserPermissionId,
                UserName = e.User.UserFullName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("User Permission Not Found");

        return Ok(ApiResponse<List<UserPermissionDto>>.Success(entity,$"Get All Succeeded"));
    }

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add(UserPermissionDtoAdd dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.UserPermissions
            .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && e.UserId == dto.UserId && !e.IsDeleted);

        if (entity != null)
            throw new ArgumentException("User Permission Exist");

        var entityPermission = await _context.Permissions
            .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Permission Not Found");

        var entityUser = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Not Found");


        string text;

        if (dto.UserPermissionId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditUserPermission");
            entity = await _context.UserPermissions
                .FirstOrDefaultAsync(e => e.UserPermissionId == dto.UserPermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("User Permission ID Not Found");

            if (entity.PermissionId == dto.PermissionId && entity.UserId == dto.UserId)
            {
                text = "User Permission Exist";
            }
            else
            {
                var logId = await _logService.Log("Edit",nameof(UserPermission), dto.UserPermissionId.Value);

                if (entity.PermissionId != dto.PermissionId && dto.PermissionId.HasValue)
                {
                    await _logService.ChangLog(
                        logId,
                        entity.PermissionId.ToString(),
                        dto.PermissionId.ToString(),
                        nameof(entity.PermissionId)
                    );
                    entity.PermissionId = dto.PermissionId.Value;
                }

                if (entity.UserId != dto.UserId && dto.UserId.HasValue)
                {
                    await _logService.ChangLog(
                        logId,
                        entity.UserId.ToString(),
                        dto.UserId.ToString(),
                        nameof(entity.PermissionId)
                    );
                    entity.UserId = dto.UserId.Value;
                }

                text = "Edit Succeeded";
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddUserPermission");
            entity = new UserPermission
            {
                UserPermissionId = Guid.NewGuid(),
                PermissionId = dto.PermissionId.Value,
                UserId = dto.UserId.Value
            };

            var logId = await _logService.Log("Create", nameof(UserPermission), entity.UserPermissionId);

            await _logService.ChangLog(
                logId,
                "null",
                entity.UserId.ToString(),
                nameof(entity.UserId)
            );

            await _logService.ChangLog(
                logId,
                "null",
                entity.PermissionId.ToString(),
                nameof(entity.UserPermissionId)
            );
            

            await _context.UserPermissions.AddAsync(entity);
            text = "Add Succeeded";
        }

        var entityDto = new UserPermissionDto
        {
            UserPermissionId = entity.UserPermissionId,
            UserId = entity.UserId,
            PermissionId = entity.PermissionId,
            UserName = entityUser.UserFullName,
            PermissionName = entityPermission.PermissionName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserPermissionDto>.Success(entityDto,$"{text}"));
    }

    [Authorize]
    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteUserPermission");

        //////////////////////////////////////////////////////


        var entity = await _context.UserPermissions
            .FirstOrDefaultAsync(e => e.UserPermissionId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("delete", nameof(UserPermission), entity.UserPermissionId);

        var newTU = new UserPermissionDto
        {
            PermissionId = entity.PermissionId,
            UserId = entity.UserId,
            PermissionName = entity.Permission.PermissionName,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserPermissionDto>.Success(newTU,$"Delete Succeeded"));
    }
}