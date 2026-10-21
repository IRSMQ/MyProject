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
[Route("api/[controller]")]
public class UserPermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public UserPermissionController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
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

    [HttpGet("getbyid")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.UserPermissions
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Permission)
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

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add(UserPermissionDtoAdd dto)
    {

        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tokenRoleId = Guid.Parse(User.FindFirstValue(ClaimTypes.Role));

        await _logService.CheckToken(tokenUserId,tokenRoleId, "AddUserPermission");

        //////////////////////////////////////////////////////
        
        var entity = await _context.UserPermissions
            .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && e.UserId == dto.UserId && !e.IsDeleted);

        if (entity != null)
            throw new InvalidOperationException("User Permission Exist");

        var entityPermission = await _context.Permissions
            .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Permission Not Found");

        var entityUser = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Not Found");


        string text;

        if (dto.UserPermissionId.HasValue)
        {
            entity = await _context.UserPermissions
                .FindAsync(dto.UserPermissionId)
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
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tokenRoleId = Guid.Parse(User.FindFirstValue(ClaimTypes.Role));

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteUserPermission");

        //////////////////////////////////////////////////////


        var entity = await _context.UserPermissions
            .FindAsync(id)
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