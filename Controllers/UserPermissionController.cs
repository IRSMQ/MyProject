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
    private readonly IAuthorizationService _authorizationService;

    public UserPermissionController(
        ProjectManagementSystemContext context,
        LogService logService,
        IAuthorizationService authorizationService
        )
    {
        _context = context;
        _logService = logService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
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

        if (entity.Count() <= 0)
            throw new KeyNotFoundException("User Permission Empity");

        return Ok(ApiResponse<List<UserPermissionDto>>.Success(entity, $"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
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

    [HttpGet]
    public async Task<IActionResult> GetByUserId([FromQuery] Guid id)
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

    [HttpGet]
    public async Task<IActionResult> GetByPermissionId([FromQuery] Guid id)
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
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UserPermissionDtoAdd dto)
    {
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


        string message = string.Empty;
        string text = string.Empty;

        if (dto.UserPermissionId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "UserPermission.Create")).Succeeded)
                return Forbid();
            
            entity = await _context.UserPermissions
                .FirstOrDefaultAsync(e => e.UserPermissionId == dto.UserPermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("User Permission ID Not Found");

            if (entity.PermissionId == dto.PermissionId && entity.UserId == dto.UserId)
            {
                message = "User Permission Exist";
            }
            else
            {
                if (entity.UserId != dto.UserId && dto.UserId.HasValue)
                {
                    entity.UserId = dto.UserId.Value;
                }

                message = "Edit Succeeded";
                text = "Edit";
            }
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "UserPermission.Create")).Succeeded)
                return Forbid();

            entity = new UserPermission
            {
                UserPermissionId = Guid.NewGuid(),
                PermissionId = dto.PermissionId!.Value,
                UserId = dto.UserId!.Value
            };

            await _context.UserPermissions.AddAsync(entity);

            message = "Add Succeeded";
            text = "Create";
        }

        var entityDto = new UserPermissionDto
        {
            UserPermissionId = entity.UserPermissionId,
            UserId = entity.UserId,
            PermissionId = entity.PermissionId,
            UserName = entityUser.UserFullName,
            PermissionName = entityPermission.PermissionName
        };

        var entry = _context.Entry(entity);

        await _logService.Log(text, nameof(UserPermission), entity.UserPermissionId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserPermissionDto>.Success(entityDto, $"{message}"));
    }

    [Authorize(Policy = "UserPermission.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteID dto)
    {
        var entity = await _context.UserPermissions
            .Include(e => e.Permission)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserPermissionId == dto.ID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(UserPermission), entity.UserPermissionId);

        var newTU = new UserPermissionDto
        {
            UserPermissionId = entity.UserPermissionId,
            PermissionId = entity.PermissionId,
            UserId = entity.UserId,
            PermissionName = entity.Permission.PermissionName,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserPermissionDto>.Success(newTU,$"Delete Succeeded"));
    }
}