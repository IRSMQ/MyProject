
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.Models;

namespace Test26.Service;

public class LogService
{
    private readonly ProjectManagementSystemContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogService(ProjectManagementSystemContext context, IHttpContextAccessor IHTTP)
    {
        _context = context;
        _httpContextAccessor = IHTTP;
    }

    public async Task<Guid> Log(string logTypeName, string tableName, Guid relatedId)
    {
        
        var userIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier);
        
        Guid? userId = null;

        if (userIdClaim != null)
            userId = Guid.Parse(userIdClaim);
            
        
        var logId = Guid.NewGuid();
        var newLog = new Log
        {
            LogId = logId,
            LogType = logTypeName,
            RelatedTableName = tableName,
            RelatedId = relatedId,
            UserId = userId,
            Date = DateTime.Now
        };

        await _context.Logs.AddAsync(newLog);
        return logId;
    }

    public async Task ChangLog(Guid logID, string oldValue, string newValue, string columnName)
    {
        var ncl = new ChangeLog
        {
            LogId = logID,
            ColumnName = columnName,
            OldValue = oldValue,
            NewValue = newValue
        };
        
        await _context.ChangeLogs.AddAsync(ncl);
    }

    public async Task<bool> CheckToken(Guid tokenUserId, Guid? tokenRoleId, string PermissionTableName){

        if (string.IsNullOrEmpty(tokenRoleId.ToString()))
            throw new InvalidOperationException("Token Role Is Null");

        var entityRole = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == tokenRoleId)
            ?? throw new KeyNotFoundException("Role Not Found");

        if (entityRole.RoleName != "Manager")
        {
            var entityRolePermission = await _context.RolePermissions
                .Include(e => e.Permission)
                .Where(e => e.RoleId == tokenRoleId && !e.IsDeleted)
                .ToListAsync();

            var entityUserPermission = await _context.UserPermissions
                .Include(e => e.Permission)
                .Where(e => e.UserId == tokenUserId && !e.IsDeleted)
                .ToListAsync();

            bool check1 = false;
            string check2 = "null";

            if (entityRolePermission.Count > 0)
            {
                foreach (var e in entityRolePermission)
                {
                    if (e.Permission.PermissionName == PermissionTableName)
                        check1 = true;
                }
            }

            if (entityUserPermission.Count() > 0)
            {
                foreach (var e in entityUserPermission)
                {
                    if (e.Permission.PermissionName == PermissionTableName)
                    {
                        check2 = e.Bit.ToString();
                    }
                }
            }

            if (check2 == "null")
                if (check1)
                    return true;
                else throw new InvalidOperationException("This user does not have this level of access");

            else
                if (check2 == "true")
                    return true;
                else throw new InvalidOperationException("This user does not have this level of access");
        }
        else
        {
            return true;
        }
    }
}