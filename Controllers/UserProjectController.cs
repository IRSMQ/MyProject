using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Test26.Service;
using Test26.ApiR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]")]
public class UserProjectController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public UserProjectController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
            .Select(e => new UserProjectDto
            {
                UserId = e.UserId,
                ProjectId = e.UserProjectId,
                UserName = e.User.UserFullName,
                ProjectName = e.Project.ProjectName
            })
            .ToListAsync();

        if (entity.Count() > 0)
            throw new KeyNotFoundException("User Project Empity");

        return Ok(ApiResponse<List<UserProjectDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet("getbyid")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.UserProjects
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Project)
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

    [HttpPost("add")]
    public async Task<IActionResult> Add(UserProjectDto dto)
    {
        var exist = await _context.UserProjects
            .AnyAsync(e => e.ProjectId == dto.ProjectId && e.UserId == dto.UserId);

        if (exist)
            throw new InvalidOperationException("User Project Exist");

        var existProject = await _context.Projects
            .AnyAsync(e => e.ProjectId == dto.ProjectId);

        if (!existProject)
            throw new KeyNotFoundException("Project Not Found");

        var existUser = await _context.Users
            .AnyAsync(e => e.UserId == dto.UserId);

        if (!existUser)
            throw new KeyNotFoundException("User Not Found");


        var PUD = new UserProjectDto();
        string text;

        if (dto.UserProjectId.HasValue)
        {
            var entity = await _context.UserProjects
                .FindAsync(dto.UserProjectId)
                ?? throw new KeyNotFoundException("User Project ID Not Found");

            var logId = await _logService.Log("Edit",nameof(UserProject), dto.UserProjectId.Value);

            if (entity.ProjectId != dto.ProjectId && dto.ProjectId.HasValue)
            {
                await _logService.ChangLog(
                    logId,
                    entity.ProjectId.ToString(),
                    dto.ProjectId.ToString(),
                    nameof(entity.ProjectId)
                );
                entity.ProjectId = dto.ProjectId.Value;
            }

            if (entity.UserId != dto.UserId && dto.UserId.HasValue)
            {
                await _logService.ChangLog(
                    logId,
                    entity.UserId.ToString(),
                    dto.UserId.ToString(),
                    nameof(entity.ProjectId)
                );
                entity.UserId = dto.UserId.Value;
            }

            PUD.ProjectId = entity.ProjectId;
            PUD.UserId = entity.UserId;
            PUD.ProjectName = entity.Project.ProjectName;
            PUD.UserName = entity.User.UserFullName;
            text = "Add Succeeded";
        }
        else
        {
            var newTU = new UserProject
            {
                UserProjectId = Guid.NewGuid(),
                ProjectId = dto.ProjectId.Value,
                UserId = dto.UserId.Value
            };

            await _context.UserProjects.AddAsync(newTU);
            
            var entry = _context.Entry(newTU);

            var logId = await _logService.Log("Create", nameof(UserProject), newTU.UserProjectId);

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name
                    );
                }
            }

            PUD.ProjectId = newTU.ProjectId;
            PUD.UserId = newTU.UserId;
            PUD.ProjectName = newTU.Project.ProjectName;
            PUD.UserName = newTU.User.UserFullName;
            text = "Edit Succeeded";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserProjectDto>.Success(PUD,$"{text}"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.UserProjects
            .FindAsync(id)
            ?? throw new KeyNotFoundException("User Project Not Found");

        entity.IsDeleted = true;

        await _logService.Log("delete", nameof(UserProject), entity.UserProjectId);

        var newTU = new UserProjectDto
        {
            ProjectId = entity.ProjectId,
            UserId = entity.UserId,
            ProjectName = entity.Project.ProjectName,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserProjectDto>.Success(newTU,$"Delete Succeeded"));
    }
}