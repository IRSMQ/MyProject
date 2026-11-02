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
    private readonly TokenService _tokenService;

    public RolePermissionController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
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
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] RPDtoAdd dto)
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
                throw new ArgumentException($"This permission available for this Role");

        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.RPID.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "EditRolePermission");

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
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddRolePermission");

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

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeleteRolePermission");

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
