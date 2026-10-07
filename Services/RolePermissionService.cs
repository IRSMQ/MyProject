/*
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;

namespace Test26.Service;


public interface IRolePermissionService
{
    
}

public class RolePermissionService
{
    private readonly ProjectManagementSystemContext _context;
    public RolePermissionService(ProjectManagementSystemContext context)
    {
        _context = context;
    }

    public async Task<List<RPDto>> GetAll()
    {
        var rp = await _context.RolePermissions
        .Select(r => new RPDto
        {
            RoleID = r.RoleId,
            PermissionID = r.PermissionId,
            RoleName = r.Role.RoleName,
            PermissionName = r.Permission.PermissionName
        })
        .ToListAsync();

        if (rp.Count == 0)
            throw new InvalidOperationException($"There are no RolePermissions");
    
        return rp;
    }

    public async Task<List<RPDto>> GetByRoleId(Guid id)
    {
        var rp = await _context.RolePermissions
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

        return rp;
    }

    public async Task<List<RPDto>> GetByPermissionId(Guid id)
    {
        var rp = await _context.RolePermissions
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

        return rp;
    }

    public async Task<RPDto> Add(RPDtoAdd rPDtoAdd)
    {

        var r = await _context.Roles.FindAsync(rPDtoAdd.RoleID);
        var p = await _context.Permissions.FindAsync(rPDtoAdd.PermissionID);

        if (r == null || p == null)
            throw new KeyNotFoundException($"Role ID or Permition ID NULL");

        var rp = await _context.RolePermissions
            .AnyAsync(r => r.RoleId == rPDtoAdd.RoleID && r.PermissionId == rPDtoAdd.PermissionID);
            
        if (rp)
            throw new InvalidOperationException($"This permission is available for this role Role: {r.RoleName} Permission: {p.PermissionName}");

        var newRp = new RolePermission
        {
            RoleId = rPDtoAdd.RoleID,
            PermissionId = rPDtoAdd.PermissionID
        };

        _context.RolePermissions.Add(newRp);
        await _context.SaveChangesAsync();

        return new RPDto
        {
            RoleID = rPDtoAdd.RoleID,
            PermissionID = rPDtoAdd.PermissionID,
            RoleName = r.RoleName,
            PermissionName = p.PermissionName
        };
    }

    public async Task<RPDto> Delete(RPDtoAdd rPDtoAdd)
    {

        var r = await _context.Roles.FindAsync(rPDtoAdd.RoleID);

        if (r == null)
            throw new KeyNotFoundException($"Role Not Exist");

        var p = await _context.Permissions.FindAsync(rPDtoAdd.PermissionID);

        if (p == null)
            throw new KeyNotFoundException($"Permission Not Exist");

        var rp = await _context.RolePermissions
            .Where(r => r.RoleId == rPDtoAdd.RoleID && r.PermissionId == rPDtoAdd.PermissionID)
            .FirstOrDefaultAsync();

        if (rp == null)
            throw new KeyNotFoundException($"RoleID or PermissionID Not Exist");

        _context.RolePermissions.Remove(rp);
        await _context.SaveChangesAsync();

        return new RPDto
        {
            RoleID = rPDtoAdd.RoleID,
            PermissionID = rPDtoAdd.PermissionID,
            RoleName = r.RoleName,
            PermissionName = p.PermissionName
        };
    }
}
*/