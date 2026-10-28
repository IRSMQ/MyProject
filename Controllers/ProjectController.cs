using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.Models;
using Test26.Service;
using Test26.DTOs;
using Test26.ApiR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace Test26.Controller;
[ApiController]
[Route("api/[controller]/[action]")]
public class ProjectController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public ProjectController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _context.Projects
            .Include(e => e.Status)
            .Include(e => e.Manager)
            .Include(e => e.Priority)
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Select(e => new ProjectDto
            {
                ProjectId = e.ProjectId,
                StatusId = e.StatusId,
                ManagerId = e.ManagerId,
                PriorityId = e.PriorityId,
                ProjectName = e.ProjectName,
                Description = e.Desc,
                StartDate = e.StartDate,
                DueDate = e.DueDate,
                EndDate = e.EndDate,
                CreationDate = e.CreationDate,
                StatusName = e.Status.StatusName,
                ManagerName = e.Manager.UserFullName,
                PriorityName = e.Priority.PriorityName
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ProjectDto>>.Success(projects,$"Get All Succeeded"));
    }

    [HttpGet("id/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _context.Projects
            .Include(e => e.Status)
            .Include(e => e.Manager)
            .Include(e => e.Priority)
            .AsNoTracking()
            .Where(e => e.ProjectId == id && !e.IsDeleted)
            .Select(e => new ProjectDto
            {
                ProjectId = e.ProjectId,
                StatusId = e.StatusId,
                ManagerId = e.ManagerId,
                PriorityId = e.PriorityId,
                ProjectName = e.ProjectName,
                Description = e.Desc,
                StartDate = e.StartDate,
                DueDate = e.DueDate,
                EndDate = e.EndDate,
                CreationDate = e.CreationDate,
                StatusName = e.Status.StatusName,
                ManagerName = e.Manager.UserFullName,
                PriorityName = e.Priority.PriorityName
            })
            .FirstOrDefaultAsync();

        if (entity == null)
            throw new KeyNotFoundException($"Project Not Found");

        return Ok(ApiResponse<ProjectDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CreateProject dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ArgumentException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Projects
            .FirstOrDefaultAsync(e => e.ProjectName == dto.ProjectName && !e.IsDeleted);

        if (entity != null)
            throw new ArgumentException("Project already exists.");        

        entity = new Project();

        if (dto.ProjectId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditProject");
            if (dto.Nullable)
            {
                entity.ManagerId = dto.ManagerId;
                entity.PriorityId = dto.PriorityId;
                entity.StatusId = dto.StatusId;
                entity.Desc = dto.Description;
            }

            if (dto.ManagerId.HasValue)
            {
                var entityUser = await _context.Users
                    .FirstOrDefaultAsync(e => e.UserId == dto.ManagerId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("Manager Not Found");

                await _logService.CheckToken(entityUser.UserId, entityUser.RoleId, "ProjectManager");

                entity.ManagerId = dto.ManagerId.Value;
            }

            if (dto.PriorityId.HasValue)
            {
                var entityPriority = await _context.Priorities
                    .FirstOrDefaultAsync(e => e.PriorityId == dto.PriorityId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("Priority Not Found");

                entity.PriorityId = dto.PriorityId;
            }

            if (dto.StatusId.HasValue)
            {
                var entityStatus = await _context.Statuses
                    .FirstOrDefaultAsync(e => e.StatusId == dto.StatusId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("Status Not Found");

                entity.StatusId = dto.StatusId;
            }

            if (!string.IsNullOrWhiteSpace(dto.ProjectName))
            {
                var existsProject = await _context.Projects
                    .FirstOrDefaultAsync(e => e.ProjectName == dto.ProjectName && e.ProjectId != entity.ProjectId && !e.IsDeleted)
                    ?? throw new ArgumentException("Project Name Exist");

                entity.ProjectName = dto.ProjectName;
            }

            entity.Desc = dto.Description ?? entity.Desc;

            var entry = _context.Entry(entity);

            var modifiedProps = entry.Properties
                .Where(p => p.IsModified)
                .Select(p => new
                {
                    ColumnName = p.Metadata.Name,
                    OldVal = p.OriginalValue?.ToString() ?? "null",
                    NewVal = p.CurrentValue?.ToString() ?? "null"
                })
                .ToList();

            if (modifiedProps.Any())
            {
                var logId = await _logService.Log("Edit", nameof(Project), entity.ProjectId);

                foreach (var change in modifiedProps)
                    await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
            }
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddProject");

            if (dto.ManagerId.HasValue)
            {
                var entityUser = await _context.Users
                    .Include(e => e.Role)
                    .FirstOrDefaultAsync(e => e.UserId == dto.ManagerId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("User Not Found");

                if (entityUser.RoleId == null)
                    throw new ArgumentException("Role Is Null");

                if (entityUser.Role.RoleName != "Manager")
                    throw new ArgumentException("Role Is Not Manager");
            }

            if (dto.StartDate.HasValue && dto.CreationDate.HasValue)
            {
                if (dto.StartDate < dto.CreationDate)
                {
                    throw new ArgumentException("Start Date < Creation Date");
                }
            }

            if (dto.EndDate.HasValue && dto.StartDate.HasValue)
            {
                if (dto.EndDate < dto.StartDate)
                {
                    throw new ArgumentException("End Date < Start Date");
                }
            }

            if (dto.DueDate.HasValue && dto.EndDate.HasValue)
            {
                if (dto.DueDate > dto.EndDate)
                {
                    throw new ArgumentException("Due Date > End Date");
                }
            }

            if (dto.DueDate.HasValue && dto.StartDate.HasValue)
            {
                if (dto.DueDate < dto.StartDate)
                {
                    throw new ArgumentException("Due Date < Start Date");
                }
            }

            entity = new Project
            {
                ProjectId = Guid.NewGuid(),
                StatusId = dto.StatusId,
                ManagerId = dto.ManagerId,
                PriorityId = dto.PriorityId,
                ProjectName = dto.ProjectName,
                Desc = dto.Description,
                CreationDate = dto.CreationDate,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                DueDate = dto.DueDate,
                IsDeleted = false
            };

            
            var logId = await _logService.Log("Create", nameof(Project), entity.ProjectId);

            var entry = _context.Entry(entity);
            
            foreach (var prop in entry.Properties)
            {
                if (prop.CurrentValue != null && prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        oldValue: "null",
                        newValue: prop.CurrentValue?.ToString() ?? "null",
                        columnName: prop.Metadata.Name
                    );
                }
            }

            await _context.Projects.AddAsync(entity);
        }

        

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ProjectDto>.Success(ProjectToDto(entity),$"Add Succeeded"));
    }

    [Authorize]
    [HttpPut("edit/date")]
    public async Task<IActionResult> EditDate([FromBody] EditAllDate dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditDateProject");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Projects
            .FirstOrDefaultAsync(e => e.ProjectId == dto.ID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Project Not Found");

        var entityTaskManagement = await _context.TaskManagements
            .Where(e => e.ProjectId == entity.ProjectId)
            .ToListAsync();

        if (dto.NullDate)
        {
            entity.CreationDate = dto.CreationDate;
            entity.EndDate = dto.EndDate;
            entity.StartDate = dto.StartDate;
            entity.DueDate = dto.DueDate;
        }

        if (dto.CreationDate.HasValue)
        {
            if (dto.CreationDate > entity.StartDate || dto.CreationDate > entity.EndDate)
                throw new ArgumentException("End/Start Date < Create Date");

            foreach (var e in entityTaskManagement)
            {
                if (e.CreationDate < dto.CreationDate)
                    throw new ArgumentException("TM Creation Date < Project Creation Date");
            }
            entity.CreationDate = dto.CreationDate;
        }

        if (dto.StartDate.HasValue)
        {
            if (dto.StartDate > entity.EndDate || dto.StartDate < entity.CreationDate)
                throw new ArgumentException("Start Date > End Date or Start Date < Creation Date");

            foreach (var e in entityTaskManagement)
            {
                if (dto.StartDate > e.StartDate)
                    throw new ArgumentException("Start Date Project > Start Date Task");
            }

            entity.StartDate = dto.StartDate;
        }

        if (dto.DueDate.HasValue)
        {
            if (dto.DueDate > entity.EndDate || dto.DueDate < entity.CreationDate)
                throw new ArgumentException("Due Date > End Date or Due Date < Creation Date");

            entity.DueDate = dto.DueDate;
        }

        if (dto.EndDate.HasValue)
        {
            if (dto.EndDate < entity.CreationDate || dto.EndDate < entity.StartDate)
                throw new ArgumentException("End Date < Creation Date or End Date < Start Date");

            foreach (var e in entityTaskManagement)
                if (e.CompletionDate > dto.EndDate)
                    throw new ArgumentException("Task Completion Date > Project Completion Date");

            entity.EndDate = dto.EndDate;
        }

        var entry = _context.Entry(entity);

        var modifiedProps = entry.Properties
            .Where(e => e.IsModified)
            .Select(e => new
            {
                ColumnName = e.Metadata.Name,
                OldVal = e.OriginalValue?.ToString() ?? "null",
                NewVal = e.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        if (modifiedProps.Any())
        {
            var tableName = _context.Model
                .FindEntityType(typeof(Project))?
                .GetTableName() ?? nameof(Project);

            var logId = await _logService.Log("Edit", tableName, entity.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ProjectDto>.Success(ProjectToDto(entity),$"Edit Date Succeeded"));
    }

    [Authorize]
    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ArgumentException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteProject");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Projects
            .FirstOrDefaultAsync(e => e.ProjectId == id && !e.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Project Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Project), entity.ProjectId);

        /////////////////////////////////////////////////////////
        var entityUserProject = await _context.UserProjects
            .Where(e => e.ProjectId == id && !e.IsDeleted)
            .ToListAsync();

        if (entityUserProject.Count() > 0)
        {
            foreach (var e in entityUserProject)
            {
                await _logService.Log("Delete", nameof(UserProject), e.UserProjectId);
                e.IsDeleted = true;
            }
        }

        /////////////////////////////////////////////////////////
        var entityTaskManagement = await _context.TaskManagements
            .Where(e => e.ProjectId == id && !e.IsDeleted)
            .ToListAsync();

        if (entityTaskManagement.Count() > 0)
        {
            foreach (var e in entityTaskManagement)
            {
                await _logService.Log("Delete", nameof(TaskManagement), e.TaskManagementId);
                e.IsDeleted = true;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ProjectDto>.Success(ProjectToDto(entity),$"Delete Succeeded"));
    }


    private ProjectDto ProjectToDto(Project entity)
    {
        return new ProjectDto
        {
            ProjectId = entity.ProjectId,
            StatusId = entity.StatusId,
            ManagerId = entity.ManagerId,
            PriorityId = entity.PriorityId,
            ProjectName = entity.ProjectName,
            Description = entity.Desc,
            StartDate = entity.StartDate,
            DueDate = entity.DueDate,
            EndDate = entity.EndDate,
            CreationDate = entity.CreationDate,
            StatusName = entity.Status?.StatusName,
            ManagerName = entity.Manager?.UserFullName,
            PriorityName = entity.Priority?.PriorityName
        };
    }
}