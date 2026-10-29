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
    private readonly TokenService _tokenService;

    public RoleController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
    }

    [HttpGet]
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

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
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
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] RoleDto dto)
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

        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.RoleId.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "EditRole");
            entity = await _context.Roles
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Role Not Found");

            if (entity.RoleName == dto.RoleName)
            {
                message = "Role Exist";
            }
            else
            {
                entity.RoleName = dto.RoleName;
                message = "Edit Succeeded";
                textLog = "Edit";
            }
        }
        else
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddRole");
            entity = new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = dto.RoleName,
                IsDeleted = false
            };

            await _context.Roles.AddAsync(entity);

            await _context.AddAsync(entity);
            message = "Add Succeeded";
            textLog = "Create";
        }

        var newEntity = new RoleDto
        {
            RoleId = entity.RoleId,
            RoleName = entity.RoleName
        };

        var entry = _context.Roles.Entry(entity);
        await _logService.Log(textLog, nameof(Role), entity.RoleId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RoleDto>.Success(newEntity, $"{message}"));
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

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeleteRole");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Role Not Found");


        /////////////////////////////////////////////////////////
        var entityUser = await _context.Users
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityUser)
        {
            e.RoleId = null;
            var entry = _context.Users.Entry(e);
            await _logService.Log("Edit", nameof(User), e.UserId, entry);
        }


        /////////////////////////////////////////////////////////
        var entityRolePermission = await _context.RolePermissions
            .Where(e => e.RoleId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityRolePermission)
        {
            e.IsDeleted = true;
            await _logService.Log("Delete", nameof(RolePermission), e.RolePermissionId);
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
