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


[Route("api/[controller]/[action]")]
[ApiController]
public class LogController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly TokenService _tokenService;

    public LogController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNTop([FromQuery] int n)
    {
        var entity = await _context.Logs
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Take(n)
            .Select(e => new LogDto
            {
                logId = e.LogId,
                relatedId = e.RelatedId,
                userId = e.UserId,
                logType = e.LogType,
                relatedTableName = e.RelatedTableName,
                Date = e.Date
            })
            .ToListAsync()
            ?? throw new ArgumentException("Change Log Empity");

        return Ok(ApiResponse<List<LogDto>>.Success(entity,$"Get N Top Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.Logs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.LogId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        var newEntity = new LogDto
        {
            logId = entity.LogId,
            relatedId = entity.RelatedId,
            userId = entity.UserId,
            logType = entity.LogType,
            relatedTableName = entity.RelatedTableName,
            Date = entity.Date
        };

        return Ok(ApiResponse<LogDto>.Success(newEntity, $"Get By ID Succeeded"));
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

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeleteLog");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Logs
            .FirstOrDefaultAsync(e => e.LogId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Log), id);

        var entityChangeLog = await _context.ChangeLogs
            .Where(e => e.LogId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityChangeLog)
        {
            e.IsDeleted = true;
            await _logService.Log("Delete", nameof(ChangeLog), e.ChangeLogId);
        }

        var newEntity = new LogDto
        {
            logId = entity.LogId,
            relatedId = entity.RelatedId,
            userId = entity.UserId,
            logType = entity.LogType,
            relatedTableName = entity.RelatedTableName,
            Date = entity.Date
        };
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<LogDto>.Success(newEntity,$"Delete Succeeded"));
    }
}