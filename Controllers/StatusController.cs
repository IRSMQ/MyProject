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
[Route("api/[controller]/[action]")]
public class StatusController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly TokenService _tokenService;

    public StatusController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
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
    [HttpPut]
    public async Task<ActionResult<StatusDto>> Put([FromBody] StatusAddDto dto)
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

        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.StatusId.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "EditStatus");

            entity = await _context.Statuses
                .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId && !p.IsDeleted)
                ?? throw new KeyNotFoundException($"Status Not Found");

            if (entity.StatusName == dto.StatusName)
            {
                message = "Status Exist";
            }
            else
            {
                entity.StatusName = dto.StatusName;
                message = "Edit Succeeded";
                textLog = "Edit";
            }
        }
        else
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddStatus");
            entity = new Status
            {
                StatusId = Guid.NewGuid(),
                StatusName = dto.StatusName,
                IsDeleted = false
            };

            await _context.Statuses.AddAsync(entity);
            
            message = "Add Succeeded";
            textLog = "Create";
        }
        
        var newStatus = new StatusDto
        {
            StatusId = entity.StatusId,
            StatusName = entity.StatusName
        };

        var entry = _context.Statuses.Entry(entity);
        
        await _logService.Log(textLog, nameof(Status), entity.StatusId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<StatusDto>.Success(newStatus, $"{message}"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult<StatusDto>> Delete([FromBody] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeleteStatus");

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
                e.StatusId = null;
                var entry = _context.Projects.Entry(e);
                await _logService.Log("Edit", nameof(Project), e.ProjectId, entry);                
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
                e.StatusId = null;
                var entry = _context.TaskManagements.Entry(e);
                await _logService.Log("Edit", nameof(TaskManagement), e.TaskManagementId, entry);
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
