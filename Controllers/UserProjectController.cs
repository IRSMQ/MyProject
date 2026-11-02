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
    private readonly TokenService _tokenService;

    public UserProjectController(ProjectManagementSystemContext context, LogService logService, TokenService tokenService)
    {
        _context = context;
        _logService = logService;
        _tokenService = tokenService;
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

        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        ///////////////////////////////////////////////////////////////
        
        var entity = await _context.UserProjects
            .FirstOrDefaultAsync(e => e.ProjectId == dto.ProjectId && e.UserId == dto.UserId && !e.IsDeleted);

        if (entity != null)
            throw new ArgumentException("User Project Exist");

        var entityProject = await _context.Projects
            .FirstOrDefaultAsync(e => e.ProjectId == dto.ProjectId && !e.IsDeleted);

        if (entityProject == null)
            throw new KeyNotFoundException("Project Not Found");

        var entityUser = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted);

        if (entityUser == null)
            throw new KeyNotFoundException("User Not Found");

        string message = string.Empty;
        string text = string.Empty;

        if (dto.UserProjectId.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "EditUserProject");

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
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddUserProject");
            var newTU = new UserProject
            {
                UserProjectId = Guid.NewGuid(),
                ProjectId = dto.ProjectId.Value,
                UserId = dto.UserId.Value
            };

            entity.ProjectId = newTU.ProjectId;
            entity.UserId = newTU.UserId;
            message = "Add Succeeded";
            text = "Create";
        }

        await _context.UserProjects.AddAsync(entity);

        var newUserProjectDto = new UserProjectDto
        {
            UserId = entity.UserId,
            ProjectId = entity.ProjectId,
            UserName = entityUser.UserFullName,
            ProjectName = entityProject.ProjectName
        };

        var entry = _context.Entry(entity);

        await _logService.Log(text, nameof(UserProject), entity.UserProjectId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserProjectDto>.Success(newUserProjectDto,$"{message}"));
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

        await _tokenService.CheckToken(tokenUserId, tokenRoleId, "DeleteUserProject");

        //////////////////////////////////////////////////////////////////////
        

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

        return Ok(ApiResponse<UserProjectDto>.Success(newTU,$"Delete Succeeded"));
    }
}