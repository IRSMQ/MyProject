using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.Models;
using Test26.Service;
using Test26.DTOs;
using Test26.ApiR;

[ApiController]
[Route("api/[controller]")]
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

    [HttpGet("id/{id}")]
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
            throw new KeyNotFoundException($"Project with ID {id} not found.");

        return Ok(ApiResponse<ProjectDto>.Success(entity,$"Get By ID Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] CreateProject dto)
    {
        var exists = await _context.Projects
            .AnyAsync(p => p.ProjectName == dto.ProjectName && !p.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Project already exists.");

        var eid = Guid.NewGuid();

        var entity = new Project
        {
            ProjectId = eid,
            ProjectName = dto.ProjectName,
            IsDeleted = false
        };

        _context.Projects.Add(entity);

        var logId = await _logService.Log("Create", nameof(Project), eid);

        var entry = _context.Entry(entity);
        
        foreach (var prop in entry.Properties)
        {
            if (prop.OriginalValue != null && prop.CurrentValue != null && prop.Metadata.Name != "IsDeleted")
            {
                await _logService.ChangLog(
                    logId,
                    oldValue: "null",
                    newValue: prop.CurrentValue?.ToString() ?? "null",
                    columnName: prop.Metadata.Name
                );
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ProjectDto>.Success(ProjectToDto(entity),$"Add Succeeded"));
    }

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] ProjectEditDto dto)
    {
        var entity = await _context.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == dto.ProjectDtoId && !r.IsDeleted)
            ?? throw new KeyNotFoundException("Project Not Found");

        if (dto.Nullable)
        {
            entity.ManagerId = dto.ManagerId;
            entity.PriorityId = dto.PriorityId;
            entity.StatusId = dto.StatusId;
            entity.Desc = dto.Description;
        }

        if (dto.ManagerId.HasValue)
        {
            var u = await _context.Users
                .Include(p => p.Role)
                .FirstOrDefaultAsync(r => r.UserId == dto.ManagerId)
                ?? throw new KeyNotFoundException("Manager Not Found");

            if (u.RoleId != Guid.Parse("6869DEFE-56C3-490A-BAB2-0EA92EC97BCA"))
                throw new InvalidOperationException("The role must be Manager");

            entity.ManagerId = dto.ManagerId.Value;
        }

        if (dto.PriorityId.HasValue)
        {
            var entityPriority = await _context.Priorities
                .FindAsync(dto.PriorityId)
                ?? throw new KeyNotFoundException("Priority Not Found");

            entity.PriorityId = dto.PriorityId;
        }

        if (dto.StatusId.HasValue)
        {
            var entityStatus = await _context.Statuses
                .FindAsync(dto.StatusId)
                ?? throw new KeyNotFoundException("Status Not Found");

            entity.StatusId = dto.StatusId;
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            var exists = await _context.Projects
                .AnyAsync(r => r.ProjectName == dto.Title && r.ProjectId != entity.ProjectId && !r.IsDeleted);

            if (exists)
                throw new InvalidOperationException("Project Name Exist");

            entity.ProjectName = dto.Title;
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
            var tableName = _context.Model
                .FindEntityType(typeof(Project))?
                .GetTableName() ?? nameof(Project);

            var logId = await _logService.Log("Edit", tableName, entity.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ProjectDto>.Success(ProjectToDto(entity),$"Edit Succeeded"));
    }

    [HttpPut("edit/date")]
    public async Task<IActionResult> EditDate([FromBody] EditAllDate dto)
    {
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
                throw new InvalidOperationException("End/Start Date < Create Date");

            foreach (var e in entityTaskManagement)
            {
                if (e.CreationDate < dto.CreationDate)
                    throw new InvalidOperationException("TM Creation Date < Project Creation Date");
            }
            entity.CreationDate = dto.CreationDate;
        }

        if (dto.StartDate.HasValue)
        {
            if (dto.StartDate > entity.EndDate || dto.StartDate < entity.CreationDate)
                throw new InvalidOperationException("Start Date > End Date or Start Date < Creation Date");

            foreach (var e in entityTaskManagement)
            {
                if (dto.StartDate > e.StartDate)
                    throw new InvalidOperationException("Start Date Project > Start Date Task");
            }

            entity.StartDate = dto.StartDate;
        }

        if (dto.DueDate.HasValue)
        {
            if (dto.DueDate > entity.EndDate || dto.DueDate < entity.CreationDate)
                throw new InvalidOperationException("Due Date > End Date or Due Date < Creation Date");

            entity.DueDate = dto.DueDate;
        }

        if (dto.EndDate.HasValue)
        {
            if (dto.EndDate < entity.CreationDate || dto.EndDate < entity.StartDate)
                throw new InvalidOperationException("End Date < Creation Date or End Date < Start Date");

            foreach (var e in entityTaskManagement)
                if (e.CompletionDate > dto.EndDate)
                    throw new InvalidOperationException("Task Completion Date > Project Completion Date");

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

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(e => e.ProjectId == id && !e.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Project with ID {id} not found.");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(Project), entity.ProjectId);

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
