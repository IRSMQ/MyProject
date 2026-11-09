using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Test26.Service;
using Test26.ApiR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]/[action]")]
public class UserProjectController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly IAuthorizationService _authorizationService;

    public UserProjectController(
        ProjectManagementSystemContext context,
        LogService logService,
        IAuthorizationService authorizationService
        )
    {
        _context = context;
        _logService = logService;
        _authorizationService = authorizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
            .Where(e => !e.IsDeleted)
            .Select(e => new UserProjectDto
            {
                UserProjectId = e.UserProjectId,
                UserId = e.UserId,
                ProjectId = e.ProjectId,
                UserName = e.User.UserFullName,
                ProjectName = e.Project.ProjectName
            })
            .ToListAsync();

        if (entity.Count() == 0)
            throw new KeyNotFoundException("User Project Empity");

        return Ok(ApiResponse<List<UserProjectDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
            .Where(e => e.UserProjectId == id && !e.IsDeleted)
            .Select(e => new UserProjectDto
            {
                UserId = e.UserId,
                ProjectId = e.UserProjectId,
                UserName = e.User.UserFullName,
                ProjectName = e.Project.ProjectName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User Project Not Found");

        return Ok(ApiResponse<UserProjectDto>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId([FromQuery] Guid id)
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
            .Where(e => e.UserId == id && !e.IsDeleted)
            .Select(e => new UserProjectDto
            {
                UserId = e.UserId,
                ProjectId = e.UserProjectId,
                UserName = e.User.UserFullName,
                ProjectName = e.Project.ProjectName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("User Project Not Found");

        return Ok(ApiResponse<List<UserProjectDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetByProjectId([FromQuery] Guid id)
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
            .Where(e => e.ProjectId == id && !e.IsDeleted)
            .Select(e => new UserProjectDto
            {
                UserId = e.UserId,
                ProjectId = e.UserProjectId,
                UserName = e.User.UserFullName,
                ProjectName = e.Project.ProjectName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("User Project Not Found");

        return Ok(ApiResponse<List<UserProjectDto>>.Success(entity,$"Get All Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UserProjectDtoAdd dto)
    {
        string message = string.Empty;
        string text = string.Empty;
        var entity = new UserProject();

        if (dto.UserProjectId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "UserProject.Edit")).Succeeded)
                return Forbid();

            entity = await _context.UserProjects
                .FirstOrDefaultAsync(e => e.UserProjectId == dto.UserProjectId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("User Project ID Not Found");

            if (entity.UserId == dto.UserId && entity.ProjectId == dto.ProjectId)
            {
                message = "User Project Exist";
            }
            else
            {
                if (entity.ProjectId != dto.ProjectId && dto.ProjectId.HasValue)
                {
                    entity.ProjectId = dto.ProjectId.Value;
                }

                if (entity.UserId != dto.UserId && dto.UserId.HasValue)
                {
                    entity.UserId = dto.UserId.Value;
                }

                entity.ProjectId = entity.ProjectId;
                entity.UserId = entity.UserId;
                message = "Edit Succeeded";
                text = "Edit";
            }
        
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "UserProject.Create")).Succeeded)
                return Forbid();

            var newTU = new UserProject
            {
                UserProjectId = Guid.NewGuid(),
                ProjectId = dto.ProjectId!.Value,
                UserId = dto.UserId!.Value
            };

            entity.ProjectId = newTU.ProjectId;
            entity.UserId = newTU.UserId;
            message = "Add Succeeded";
            text = "Create";
        }

        await _context.UserProjects.AddAsync(entity);

        var entityUser = await _context.Users.FindAsync(entity.UserId);
        var entityProject = await _context.Projects.FindAsync(entity.ProjectId);

        var newUserProjectDto = new UserProjectDto
        {
            UserId = entity.UserId,
            ProjectId = entity.ProjectId,
            UserName = entityUser?.UserFullName ?? "null",
            ProjectName = entityProject?.ProjectName ?? "null"
        };

        var entry = _context.Entry(entity);

        await _logService.Log(text, nameof(UserProject), entity.UserProjectId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserProjectDto>.Success(newUserProjectDto, $"{message}"));
    }

    [Authorize(Policy = "UserProject.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] Guid id)
    {
        var entity = await _context.UserProjects
            .Include(e => e.Project)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserProjectId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Project Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(UserProject), entity.UserProjectId);

        var newTU = new UserProjectDto
        {
            ProjectId = entity.ProjectId,
            UserId = entity.UserId,
            ProjectName = entity.Project.ProjectName,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserProjectDto>.Success(newTU, $"Delete Succeeded"));
    }
}