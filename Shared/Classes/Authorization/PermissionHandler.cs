using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Test26.Context;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ProjectManagementSystemContext _context;

    public PermissionHandler(ProjectManagementSystemContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
        )
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleIdClaim = context.User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(roleIdClaim))
            return;

        var userId = Guid.Parse(userIdClaim);
        var roleId = Guid.Parse(roleIdClaim);

        var entityRole = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == roleId);

        if (entityRole == null)
            return;

        if (entityRole.RoleName == "Manager")
        {
            context.Succeed(requirement);
            return;
        }

        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(e => e.UserId == userId
                                && e.Permission.PermissionName == requirement.Permission
                                && !e.IsDeleted
                                );

        if (userPermission != null)
        {
            if (userPermission.Bit)
                context.Succeed(requirement);
            return;
        }

        var hasRolePermission = await _context.RolePermissions
            .AnyAsync(e => e.RoleId == roleId
                    && e.Permission.PermissionName == requirement.Permission
                    && !e.IsDeleted
                    );

        if (hasRolePermission)
            context.Succeed(requirement);
        
    }   
}