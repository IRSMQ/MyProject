using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
// using Test26.Exception;

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
}