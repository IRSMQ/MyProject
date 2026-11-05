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
    private readonly IAuthorizationService _authorizationService;
    
    public PermissionController(
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
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Permission Not Found");

        return Ok(ApiResponse<PermissionDto>.Success(entity, $"Get All Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] PermissionAddDto dto)
    {
        var entity = new Permission();
        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.PermissionId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "Permission.Edit")).Succeeded)
                return Forbid();

            entity = await _context.Permissions
                .FirstOrDefaultAsync(e => e.PermissionId == dto.PermissionId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Permission Not Found");

            if (entity.PermissionName == dto.PermissionName)
            {
                message = "Permission Exist";
            }
            else
            {
                entity.PermissionName = dto.PermissionName;
                
                message = "Edit Succeeded";
                textLog = "Edit";
            }
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "Permission.Create")).Succeeded)
                return Forbid();

            entity = new Permission
            {
                PermissionId = Guid.NewGuid(),
                PermissionName = dto.PermissionName,
                IsDeleted = false
            };

            await _context.Permissions.AddAsync(entity);

            message = "Add Succeeded";
            textLog = "Create";
        }

        var newEntityDto = new PermissionDto
        {
            PermissionId = entity.PermissionId,
            PermissionName = entity.PermissionName
        };

        var entry = _context.Permissions.Entry(entity);

        await _logService.Log(textLog, nameof(Permission), entity.PermissionId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PermissionDto>.Success(newEntityDto, $"{message}"));
    }

    [Authorize(Policy = "Permission.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] Guid id)
    {
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