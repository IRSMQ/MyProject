using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Test26.Service;
using Test26.ApiR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpPost("add")]
    public async Task<IActionResult> Add(UserPermissionDto dto)
    {
        var exist = await _context.UserPermissions
            .AnyAsync(e => e.PermissionId == dto.PermissionId && e.UserId == dto.UserId);

        if (exist)
            throw new InvalidOperationException("User Permission Exist");

        var existPermission = await _context.Permissions
            .AnyAsync(e => e.PermissionId == dto.PermissionId);

        if (!existPermission)
            throw new KeyNotFoundException("Permission Not Found");

        var existUser = await _context.Users
            .AnyAsync(e => e.UserId == dto.UserId);

        if (!existUser)
            throw new KeyNotFoundException("User Not Found");


        var PUD = new UserPermissionDto();
        string text;

        if (dto.UserPermissionId.HasValue)
        {
            var entity = await _context.UserPermissions
                .FindAsync(dto.UserPermissionId)
                ?? throw new KeyNotFoundException("User Permission ID Not Found");

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

            PUD.PermissionId = entity.PermissionId;
            PUD.UserId = entity.UserId;
            PUD.PermissionName = entity.Permission.PermissionName;
            PUD.UserName = entity.User.UserFullName;
            text = "Add Succeeded";
        }
        else
        {
            var newTU = new UserPermission
            {
                UserPermissionId = Guid.NewGuid(),
                PermissionId = dto.PermissionId.Value,
                UserId = dto.UserId.Value
            };

            await _context.UserPermissions.AddAsync(newTU);
            
            var entry = _context.Entry(newTU);

            var logId = await _logService.Log("Create", nameof(UserPermission), newTU.UserPermissionId);

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name
                    );
                }
            }

            PUD.PermissionId = newTU.PermissionId;
            PUD.UserId = newTU.UserId;
            PUD.PermissionName = newTU.Permission.PermissionName;
            PUD.UserName = newTU.User.UserFullName;
            text = "Edit Succeeded";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserPermissionDto>.Success(PUD,$"{text}"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.UserPermissions
            .FindAsync(id)
            ?? throw new KeyNotFoundException("User Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("delete", nameof(UserPermission), entity.UserPermissionId);

        await _context.SaveChangesAsync();

        var newTU = new UserPermissionDto
        {
            PermissionId = entity.PermissionId,
            UserId = entity.UserId,
            PermissionName = entity.Permission.PermissionName,
            UserName = entity.User.UserFullName
        };

        return Ok(ApiResponse<UserPermissionDto>.Success(newTU,$"Delete Succeeded"));
    }
}