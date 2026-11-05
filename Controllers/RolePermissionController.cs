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
    private readonly IAuthorizationService _authorizationService;

    public RolePermissionController(
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

        return Ok(ApiResponse<RPDto>.Success(entity, $"Get By ID Succeeded"));
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

        return Ok(ApiResponse<List<RPDto>>.Success(entity, $"Get By Role ID Succeeded"));
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

        return Ok(ApiResponse<List<RPDto>>.Success(entity, $"Get By Permission ID Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] RPDtoAdd dto)
    {
        // var entity = await _context.RolePermissions
        //     .Include(e => e.Role)
        //     .Include(e => e.Permission)
        //     .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && e.PermissionId == dto.PermissionId && !e.IsDeleted);

        //     if (entity != null)
        //         throw new ArgumentException($"This permission available for this Role");

        var entity = new RolePermission();

        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.RPID.HasValue)
        {
            var result = await _authorizationService.AuthorizeAsync(User, "RolePermission.Edit");
            if (!result.Succeeded)
                return Forbid();

            entity = await _context.RolePermissions
                .FirstOrDefaultAsync(e => e.RolePermissionId == dto.RPID && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Role Permission Not Found");

            entity.RoleId = dto.RoleId;
            entity.PermissionId = dto.PermissionId;

            message = "Edit Succeeded";
            textLog = "Edit";
        }
        else
        {
            var result = await _authorizationService.AuthorizeAsync(User, "RolePermission.Create");
            if (!result.Succeeded)
                return Forbid();

            entity = new RolePermission
            {
                RolePermissionId = Guid.NewGuid(),
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            };

            await _context.RolePermissions.AddAsync(entity);

            message = "Add Succeeded";
            textLog = "Create";
        }

        var newEntityDto = new RPDto
        {
            Rpid = entity.RolePermissionId,
            RoleID = entity.RoleId,
            PermissionID = entity.PermissionId,
            RoleName = entity.Role.RoleName,
            PermissionName = entity.Permission.PermissionName
        };

        var entry = _context.RolePermissions.Entry(entity);

        await _logService.Log(textLog, nameof(RolePermission), entity.RolePermissionId, entry);
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(newEntityDto, $"{message}"));
    }


    [Authorize(Policy = "RolePermission.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] Guid id)
    {
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

        return Ok(ApiResponse<RPDto>.Success(result, $"Delete Succeeded"));
    }
}
