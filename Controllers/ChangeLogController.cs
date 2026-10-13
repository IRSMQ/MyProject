using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.Context;
using Test26.Models;
using Test26.Service;
using Microsoft.EntityFrameworkCore;
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

    [HttpGet("all/{n}")]
    public async Task<IActionResult> GetNTop(int n)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Take(n)
            .ToListAsync()
            ?? throw new InvalidOperationException("Change Log Empity");

        return Ok(ApiResponse<List<ChangeLog>>.Success(entity,$"Get N Top Succeeded"));
    }

    [HttpGet("logid/{id}")]
    public async Task<IActionResult> GetByLogId(Guid id)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .Where(e => !e.IsDeleted && e.LogId == id)
            .ToListAsync();

        if (entity.Count == 0)
            throw new KeyNotFoundException("There are no Change Log with this ID");

        return Ok(ApiResponse<List<ChangeLog>>.Success(entity,$"Get By Log ID Succeeded"));
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.ChangeLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ChangeLogId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        return Ok(ApiResponse<ChangeLog>.Success(entity,$"Get By ID Succeeded"));
    }

    [HttpDelete("id/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.ChangeLogs
            .FirstOrDefaultAsync(c => c.ChangeLogId == id && !((ISoftDeletable)c).IsDeleted)
            ?? throw new KeyNotFoundException("There are no Change Log with this ID");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(ChangeLog), id);
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ChangeLog>.Success(entity,$"Delete Succeeded"));
    }
}