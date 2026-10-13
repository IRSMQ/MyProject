using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controllers;

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

        if (dto.RPID.HasValue)
        {
            var entity = await _context.RolePermissions
                .FirstOrDefaultAsync(e => e.RoleId == dto.RoleID && e.PermissionId == dto.PermissionID && e.IsDeleted == false)
                ?? throw new KeyNotFoundException($"This permission not available for this Role");

            var entityRole = await _context.Roles
                .FindAsync(dto.RoleID)
                ?? throw new KeyNotFoundException("Role Not Found");

            var entityPermission = await _context.Permissions
                .FindAsync(dto.PermissionID)
                ?? throw new KeyNotFoundException("Permission Not Found");

            entity.RoleId = dto.RoleID;
            entity.PermissionId = dto.PermissionID;

            var logId = await _logService.Log("Edit", nameof(RolePermission), entity.Rpid);

            var entry = _context.Entry(entity);

            var modifiedProps = entry.Properties
                .Where(e => e.IsModified)
                .Select(e => new
                {
                    ColumnName = e.Metadata.Name,
                    OldValue = e.OriginalValue?.ToString() ?? "null",
                    NewValue = e.CurrentValue?.ToString() ?? "null"
                })
                .ToList();

            foreach (var prop in modifiedProps)
            {
                await _logService.ChangLog(
                    logId,
                    prop.OldValue,
                    prop.NewValue,
                    prop.ColumnName
                );
            }

            result.RoleID = dto.RoleID;
            result.PermissionID = dto.PermissionID;
            result.RoleName = entityRole.RoleName;
            result.PermissionName = entityPermission.PermissionName;

        }
        else
        {
            var RoleEntity = await _context.Roles
                .FindAsync(dto.RoleID);
            
            var PermissionEntity = await _context.Permissions
                .FindAsync(dto.PermissionID);

            if (RoleEntity == null || PermissionEntity == null)
                throw new KeyNotFoundException("Role ID or Permission ID is NULL");

            var exists = await _context.RolePermissions
                .AnyAsync(e => e.RoleId == dto.RoleID && e.PermissionId == dto.PermissionID && e.IsDeleted == false);

            if (exists)
                throw new InvalidOperationException($"This permission is available for this Role: {RoleEntity.RoleName} Permission: {PermissionEntity.PermissionName}");

            var newRp = new RolePermission
            {
                Rpid = Guid.NewGuid(),
                RoleId = dto.RoleID,
                PermissionId = dto.PermissionID
            };

            _context.RolePermissions.Add(newRp);

            var logId = await _logService.Log("Create", nameof(RolePermission), newRp.Rpid);

            var entry = _context.Entry(newRp);

            foreach(var prop in entry.Properties)
            {
                if (prop.OriginalValue != null && prop.CurrentValue != null && prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name
                    );
                }
            }

            result.Rpid = newRp.Rpid;
            result.RoleID = dto.RoleID;
            result.PermissionID = dto.PermissionID;
            result.RoleName = RoleEntity.RoleName;
            result.PermissionName = PermissionEntity.PermissionName;
        }
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<RPDto>.Success(result,$"Succeded"));
    }


    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromBody] RPDtoDelete dto)
    {
        Guid relatedId;
        RolePermission entity;
        if (dto.Rpid.HasValue)
        {
            entity = await _context.RolePermissions.FindAsync(dto.Rpid)
                ?? throw new KeyNotFoundException("Role Permission Not Found");

            entity.IsDeleted = true;
            relatedId = dto.Rpid.Value;
        }
        else if (dto.RoleID.HasValue && dto.PermissionID.HasValue)
        {
            entity = await _context.RolePermissions
                .FirstOrDefaultAsync(x => x.RoleId == dto.RoleID && x.PermissionId == dto.PermissionID)
                ?? throw new KeyNotFoundException("RoleID or PermissionID Not Exist");

            entity.IsDeleted = true;
            relatedId = entity.Rpid;
        }
        else throw new KeyNotFoundException("RoleID || PermissionID || RPID = null");

        await _logService.Log("Delete", nameof(RolePermission), relatedId);
        
        
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
