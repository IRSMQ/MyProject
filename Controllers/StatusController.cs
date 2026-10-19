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

    [HttpGet("all")]
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

    [HttpGet("byid/{id:guid}")]
    public async Task<ActionResult<StatusDto>> GetById(Guid id)
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

    [HttpPost("add")]
    public async Task<ActionResult<StatusDto>> Add([FromBody] StatusAddDto dto)
    {
        var exists = await _context.Statuses
            .AnyAsync(p => p.StatusName == dto.StatusName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Status already exists.");

        var newStatus = new StatusDto();
        string text;

        if (dto.StatusId.HasValue)
        {
            var entity = await _context.Statuses
                .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId && !p.IsDeleted)
                ?? throw new KeyNotFoundException($"Status Not Found");

            if (entity.StatusName == dto.StatusName)
            {
                newStatus.StatusId = entity.StatusId;
                newStatus.StatusName = entity.StatusName;

                return Ok(ApiResponse<StatusDto>.Success(newStatus,$"Status Exist"));
            }

            var entry = _context.Entry(entity);

            var logId = await _logService.Log("Edit", nameof(Status), entity.StatusId);

            await _logService.ChangLog(
                logId,
                entity.StatusName,
                dto.StatusName,
                nameof(entity.StatusName)
            );

            entity.StatusName = dto.StatusName;
            text = "Edit Succeeded";

            newStatus.StatusId = entity.StatusId;
            newStatus.StatusName = entity.StatusName;
        }
        else
        {
            var entity = new Status
            {
                StatusId = Guid.NewGuid(),
                StatusName = dto.StatusName,
                IsDeleted = false
            };

            _context.Statuses.Add(entity);
            
            var logId = await _logService.Log("Create", nameof(Status), entity.StatusId);

            var entry = _context.Entry(entity);
            foreach (var prop in entry.Properties)
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

            text = "Add Succeeded";

            newStatus.StatusId = entity.StatusId;
            newStatus.StatusName = entity.StatusName;
        }
        

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<StatusDto>.Success(newStatus, $"{text}"));
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<ActionResult<StatusDto>> Delete(Guid id)
    {
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
