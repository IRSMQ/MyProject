using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.Context;
using Test26.Models;
using Test26.Service;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Test26.DTOs;

namespace Test26.Controller;


[Route("api/[controller]")]
[ApiController]
public class ChangeLogController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public ChangeLogController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("top/{n}")]
    public async Task<IActionResult> GetNTop(int n)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Take(n)
            .Select(e => new ChangeLogDto
            {
                changeLogId = e.ChangeLogId,
                logId = e.LogId,
                columnName = e.ColumnName,
                oldValue = e.OldValue,
                newValue = e.NewValue
            })
            .ToListAsync()
            ?? throw new ArgumentException("Change Log Empity");

        return Ok(ApiResponse<List<ChangeLogDto>>.Success(entity, $"Get N Top Succeeded"));
    }

    [HttpGet("logid/{id}")]
    public async Task<IActionResult> GetByLogId(Guid id)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .Where(e => e.LogId == id && !e.IsDeleted)
            .Select(e => new ChangeLogDto
            {
                changeLogId = e.ChangeLogId,
                logId = e.LogId,
                columnName = e.ColumnName,
                oldValue = e.OldValue,
                newValue = e.NewValue
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException("There are no Change Log with this ID");

        return Ok(ApiResponse<List<ChangeLogDto>>.Success(entity, $"Get By Log ID Succeeded"));
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ChangeLogId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        var newEntity = new ChangeLogDto
        {
            changeLogId = entity.ChangeLogId,
            logId = entity.LogId,
            columnName = entity.ColumnName,
            oldValue = entity.OldValue,
            newValue = entity.NewValue
        };

        return Ok(ApiResponse<ChangeLogDto>.Success(newEntity, $"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpDelete("id/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteChangeLog");

        //////////////////////////////////////////////////////
        
        var entity = await _context.ChangeLogs
            .FirstOrDefaultAsync(e => e.ChangeLogId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(ChangeLog), id);

        var newEntity = new ChangeLogDto
        {
            changeLogId = entity.ChangeLogId,
            logId = entity.LogId,
            columnName = entity.ColumnName,
            oldValue = entity.OldValue,
            newValue = entity.NewValue
        };
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ChangeLogDto>.Success(newEntity, $"Delete Succeeded"));
    }
}