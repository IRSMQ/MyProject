using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;

namespace Test26.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolePermissionController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;

    public RolePermissionController(ProjectManagementSystemContext context)
    {
        _context = context;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var rp = await _context.RolePermissions
            .AsNoTracking()
            .Select(r => new RPDto
            {
                RoleID = r.RoleId,
                PermissionID = r.PermissionId,
                RoleName = r.Role.RoleName,
                PermissionName = r.Permission.PermissionName
            })
            .ToListAsync();

        if (rp.Count == 0)
            throw new InvalidOperationException("There are no RolePermissions");

        return Ok(rp);
    }


    [HttpGet("byRole/{id}")]
    public async Task<IActionResult> GetByRoleId(Guid id)
    {
        var rp = await _context.RolePermissions
            .AsNoTracking()
            .Where(r => r.RoleId == id)
            .Select(r => new RPDto
            {
                RoleID = r.RoleId,
                PermissionID = r.PermissionId,
                RoleName = r.Role.RoleName,
                PermissionName = r.Permission.PermissionName
            })
            .ToListAsync();

        if (rp.Count == 0)
            throw new KeyNotFoundException($"RolePermission With ID: {id} Not Exist");

        return Ok(rp);
    }


    [HttpGet("byPermission/{id:guid}")]
    public async Task<IActionResult> GetByPermissionId(Guid id)
    {
        var rp = await _context.RolePermissions
            .AsNoTracking()
            .Where(r => r.PermissionId == id)
            .Select(r => new RPDto
            {
                RoleID = r.RoleId,
                PermissionID = r.PermissionId,
                RoleName = r.Role.RoleName,
                PermissionName = r.Permission.PermissionName
            })
            .ToListAsync();

        if (rp.Count == 0)
            throw new KeyNotFoundException($"RolePermission With ID: {id} Not Exist");

        return Ok(rp);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] RPDtoAdd rPDtoAdd)
    {
        var r = await _context.Roles.FindAsync(rPDtoAdd.RoleID);
        var p = await _context.Permissions.FindAsync(rPDtoAdd.PermissionID);

        if (r == null || p == null)
            throw new KeyNotFoundException("Role ID or Permission ID NULL");

        var exists = await _context.RolePermissions
            .AnyAsync(x => x.RoleId == rPDtoAdd.RoleID && x.PermissionId == rPDtoAdd.PermissionID);

        if (exists)
            throw new InvalidOperationException($"This permission is available for this role Role: {r.RoleName} Permission: {p.PermissionName}");

        var newRp = new RolePermission
        {
            RoleId = rPDtoAdd.RoleID,
            PermissionId = rPDtoAdd.PermissionID
        };

        _context.RolePermissions.Add(newRp);
        await _context.SaveChangesAsync();

        var result = new RPDto
        {
            RoleID = rPDtoAdd.RoleID,
            PermissionID = rPDtoAdd.PermissionID,
            RoleName = r.RoleName,
            PermissionName = p.PermissionName
        };

        return Ok(result);
    }


    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] RPDtoAdd rPDtoAdd)
    {
        var r = await _context.Roles.FindAsync(rPDtoAdd.RoleID);
        if (r == null)
            throw new KeyNotFoundException("Role Not Exist");

        var p = await _context.Permissions.FindAsync(rPDtoAdd.PermissionID);
        if (p == null)
            throw new KeyNotFoundException("Permission Not Exist");

        var rp = await _context.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == rPDtoAdd.RoleID && x.PermissionId == rPDtoAdd.PermissionID);

        if (rp == null)
            throw new KeyNotFoundException("RoleID or PermissionID Not Exist");

        _context.RolePermissions.Remove(rp);
        await _context.SaveChangesAsync();

        var result = new RPDto
        {
            RoleID = rPDtoAdd.RoleID,
            PermissionID = rPDtoAdd.PermissionID,
            RoleName = r.RoleName,
            PermissionName = p.PermissionName
        };

        return Ok(result);
    }
}
