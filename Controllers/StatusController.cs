using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
public class StatusController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public StatusController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StatusDto>>> GetAll()
    {
        var entity = await _context.Statuses
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new StatusDto
            {
                StatusId = e.StatusId,
                StatusName = e.StatusName,
            })
            .ToListAsync();

        return Ok(entity);
    }

    [HttpGet]
    public async Task<ActionResult<StatusDto>> GetById([FromQuery] Guid id)
    {
        var entity = await _context.Statuses
            .AsNoTracking()
            .Where(e => e.StatusId == id && !e.IsDeleted)
            .Select(e => new StatusDto
            {
                StatusId = e.StatusId,
                StatusName = e.StatusName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Status Not Found");

        return Ok(ApiResponse<StatusDto>.Success(entity, $"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<StatusDto>> Add([FromQuery] StatusAddDto dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Statuses
            .FirstOrDefaultAsync(p => p.StatusName == dto.StatusName && !p.IsDeleted);

        if (entity != null)
            throw new ArgumentException("Status already exists");

        string text;

        if (dto.StatusId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditStatus");
            entity = await _context.Statuses
                .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId && !p.IsDeleted)
                ?? throw new KeyNotFoundException($"Status Not Found");

            if (entity.StatusName == dto.StatusName)
            {
                text = "Status Exist";
            }
            else
            {
                var logId = await _logService.Log("Edit", nameof(Status), entity.StatusId);

                await _logService.ChangLog(
                    logId,
                    entity.StatusName,
                    dto.StatusName,
                    nameof(entity.StatusName)
                );

                entity.StatusName = dto.StatusName;
                text = "Edit Succeeded";
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddStatus");
            entity = new Status
            {
                StatusId = Guid.NewGuid(),
                StatusName = dto.StatusName,
                IsDeleted = false
            };

            await _context.Statuses.AddAsync(entity);
            
            var logId = await _logService.Log("Create", nameof(Status), entity.StatusId);

            await _logService.ChangLog(
                    logId,
                    "null",
                    entity.StatusId.ToString(),
                    nameof(entity.StatusId)
                );

            await _logService.ChangLog(
                    logId,
                    "null",
                    dto.StatusName,
                    nameof(entity.StatusName)
                );

            text = "Add Succeeded";
        }
        

        var newStatus = new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<StatusDto>.Success(newStatus, $"{text}"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult<StatusDto>> Delete([FromQuery] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteStatus");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Statuses
            .FirstOrDefaultAsync(e => e.StatusId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Status Not Found");


        /////////////////////////////////////////////////////////
        var entityProject = await _context.Projects
            .Where(e => e.StatusId == id && !e.IsDeleted)
            .ToListAsync();

        if (entityProject.Count() > 0)
        {
            foreach (var e in entityProject)
            {
                var logId = await _logService.Log("Edit", nameof(Project), e.ProjectId);

                await _logService.ChangLog(
                    logId,
                    e.StatusId.ToString(),
                    "null",
                    nameof(e.StatusId)
                );

                e.StatusId = null;
            }
        }


        /////////////////////////////////////////////////////////
        var entityTaskManagement = await _context.TaskManagements
            .Where(e => e.StatusId == id && !e.IsDeleted)
            .ToListAsync();

        if (entityTaskManagement.Count() > 0)
        {
            foreach (var e in entityTaskManagement)
            {
                var logId = await _logService.Log("Edit", nameof(TaskManagement), e.TaskManagementId);

                await _logService.ChangLog(
                    logId,
                    e.StatusId.ToString(),
                    "null",
                    nameof(e.StatusId)
                );

                e.StatusId = null;
            }
        }

        entity.IsDeleted = true;
    
        await _logService.Log("Delete", nameof(Status), entity.StatusId);

        var newStatus = new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<StatusDto>.Success(newStatus,$"Delete Succeeded"));
    }
}
