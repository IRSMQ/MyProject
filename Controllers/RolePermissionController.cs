using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]")]
public class RolePermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public RolePermissionController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.IsDeleted == false)
            .Select(e => new RPDto
            {
                Rpid = e.Rpid,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new InvalidOperationException("There are no RolePermissions");

        return Ok(ApiResponse<List<RPDto>>.Success(entity,$"Get All Succeeded"));
    }


    [HttpGet("byRole/{id}")]
    public async Task<IActionResult> GetByRoleId(Guid id)
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.RoleId == id && e.IsDeleted == false)
            .Select(e => new RPDto
            {
                Rpid = e.Rpid,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException($"RolePermission With ID: {id} Not Exist");

        return Ok(ApiResponse<List<RPDto>>.Success(entity,$"Succeeded"));
    }


    [HttpGet("byPermission/{id:guid}")]
    public async Task<IActionResult> GetByPermissionId(Guid id)
    {
        var entity = await _context.RolePermissions
            .AsNoTracking()
            .Where(e => e.PermissionId == id && e.IsDeleted == false)
            .Select(e => new RPDto
            {
                Rpid = e.Rpid,
                RoleID = e.RoleId,
                PermissionID = e.PermissionId,
                RoleName = e.Role.RoleName,
                PermissionName = e.Permission.PermissionName
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException($"RolePermission Not Exist");

        return Ok(ApiResponse<List<RPDto>>.Success(entity,$"Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] RPDtoAdd dto)
    {
        var result = new RPDto();
        string text;

        if (dto.RPID.HasValue)
        {
            var entity = await _context.RolePermissions
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleId && e.PermissionId == dto.PermissionId && e.IsDeleted == false)
                ?? throw new KeyNotFoundException($"This permission not available for this Role");

            var entityRole = await _context.Roles
                .FindAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("Role Not Found");

            var entityPermission = await _context.Permissions
                .FindAsync(dto.PermissionId)
                ?? throw new KeyNotFoundException("Permission Not Found");

            var logId = await _logService.Log("Edit", nameof(RolePermission), entity.Rpid);

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

            result.RoleID = dto.RoleId;
            result.PermissionID = dto.PermissionId;
            result.RoleName = entityRole.RoleName;
            result.PermissionName = entityPermission.PermissionName;
            text = "Edit Succeeded";
        }
        else
        {
            var RoleEntity = await _context.Roles
                .FindAsync(dto.RoleId)
                ?? throw new KeyNotFoundException("Role Not Found");
            
            var PermissionEntity = await _context.Permissions
                .FindAsync(dto.PermissionId)
                ?? throw new KeyNotFoundException("Permission Not Found");

            var exists = await _context.RolePermissions
                .AnyAsync(e => e.RoleId == dto.RoleId && e.PermissionId == dto.PermissionId && e.IsDeleted == false);

            if (exists)
                throw new InvalidOperationException($"This Permission is available for this Role");

            var newRp = new RolePermission
            {
                Rpid = Guid.NewGuid(),
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            };

            _context.RolePermissions.Add(newRp);

            var logId = await _logService.Log("Create", nameof(RolePermission), newRp.Rpid);

            await _logService.ChangLog(
                logId,
                "null",
                dto.RoleId.ToString(),
                nameof(newRp.RoleId)
            );

            await _logService.ChangLog(
                logId,
                "null",
                dto.PermissionId.ToString(),
                nameof(newRp.PermissionId)
            );

            result.Rpid = newRp.Rpid;
            result.RoleID = dto.RoleId;
            result.PermissionID = dto.PermissionId;
            result.RoleName = RoleEntity.RoleName;
            result.PermissionName = PermissionEntity.PermissionName;
        }
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(result,$"Succeded"));
    }


    [HttpDelete("delete")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.RolePermissions
            .FindAsync(id)
            ?? throw new KeyNotFoundException("Role Permission Not Found");

        entity.IsDeleted = true;

        await _logService.Log("delete", nameof(RolePermission), entity.Rpid);

        var result = new RPDto
        {
            Rpid = entity.Rpid,
            RoleID = entity.RoleId,
            PermissionID = entity.PermissionId,
            RoleName = entity.Role.RoleName,
            PermissionName = entity.Permission.PermissionName
        };
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(result,$"Delete Succeeded"));
    }
}
