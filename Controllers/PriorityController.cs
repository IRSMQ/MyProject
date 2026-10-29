using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;
using Test26.Models;
using Test26.Context;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace Test26.Controller;

[Route("api/[controller]/[action]")]
[ApiController]
public class PriorityController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly TokenService _tokenService;
    public PriorityController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Priorities
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new PriorityDto
            {
                PriorityId = e.PriorityId,
                PriorityName = e.PriorityName,
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PriorityDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.Priorities
            .AsNoTracking()
            .Where(e => e.PriorityId == id && !e.IsDeleted)
            .Select(e => new PriorityDto
            {
                PriorityId = e.PriorityId,
                PriorityName = e.PriorityName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Priority Not Found");

        return Ok(ApiResponse<PriorityDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] PriorityAddDto dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Priorities
            .FirstOrDefaultAsync(e => e.PriorityName == dto.PriorityName && !e.IsDeleted);

        if (entity != null)
            throw new ArgumentException("Priority already exists.");

        string message;
        string textLog = string.Empty;

        if (dto.PriorityId.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "EditPriority");
            
            entity = await _context.Priorities
                .FirstOrDefaultAsync(e => e.PriorityId == dto.PriorityId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Priority Not Found");

            if (entity.PriorityName == dto.PriorityName)
            {
                message = "Priority Name Exist";
            }
            else
            {
                entity.PriorityName = dto.PriorityName;
                message = "Edit Succeeded";
                textLog = "Edit";
            }
        }
        else
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddPriority");
            entity = new Priority
            {
                PriorityId = Guid.NewGuid(),
                PriorityName = dto.PriorityName,
                IsDeleted = false
            };

            await _context.Priorities.AddAsync(entity);
            message = "Add Succeeded";
            textLog = "Crreate";
        }

        var entry = _context.Priorities.Entry(entity);

        await _logService.Log(textLog, nameof(Priority), entity.PriorityId, entry);    

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PriorityDto>.Success(PriorityToDto(entity), $"{message}"));
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

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeletePtiotity");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Priorities
            .FirstOrDefaultAsync(e => e.PriorityId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Priority Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Priority), entity.PriorityId, null);


        /////////////////////////////////////////////////////////
        var entityProject = await _context.Projects
            .Where(e => e.PriorityId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityProject)
        {
            e.Priority = null;
            var entry = _context.Projects.Entry(e);
            await _logService.Log("Edit", nameof(Project), e.ProjectId, entry);
        }


        /////////////////////////////////////////////////////////
        var entityTaskManagement = await _context.TaskManagements
            .Where(e => e.PriorityId == id && !e.IsDeleted)
            .ToListAsync();

        foreach (var e in entityTaskManagement)
        {
            e.PriorityId = null;
            var entry = _context.TaskManagements.Entry(e);
            await _logService.Log("Edit", nameof(TaskManagement), e.TaskManagementId, entry);
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<PriorityDto>.Success(PriorityToDto(entity),$"Delete Succeeded"));
    }


    private static PriorityDto PriorityToDto(Priority priority)
    {
        return new PriorityDto
        {
            PriorityId = priority.PriorityId,
            PriorityName = priority.PriorityName
        };
    }
}