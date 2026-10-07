using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.Models;
using Test26.Service;
using Test26.DTOs;

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
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Select(p => new ProjectDto
            {
                ProjectId = p.ProjectId,
                StatusId = p.StatusId,
                ManagerId = p.ManagerId,
                PriorityId = p.PriorityId,
                ProjectName = p.ProjectName,
                Description = p.Desc,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CreationDate = p.CreationDate,
                StatusName = p.Status.StatusName,
                ManagerName = p.Manager.UserFullName,
                PriorityName = p.Priority.PriorityName
            })
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var pm = await _context.Projects
            .AsNoTracking()
            .Where(p => p.ProjectId == id && !p.IsDeleted)
            .Select(p => new ProjectDto
            {
                ProjectId = p.ProjectId,
                StatusId = p.StatusId,
                ManagerId = p.ManagerId,
                PriorityId = p.PriorityId,
                ProjectName = p.ProjectName,
                Description = p.Desc,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                CreationDate = p.CreationDate,
                StatusName = p.Status.StatusName,
                ManagerName = p.Manager.UserFullName,
                PriorityName = p.Priority.PriorityName
            })
            .FirstOrDefaultAsync();

        if (pm == null)
            throw new KeyNotFoundException($"Project with ID {id} not found.");

        return Ok(pm);
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
        await _context.SaveChangesAsync();

        var logId = await _logService.Log("Create", nameof(Project), eid);

        var entry = _context.Entry(entity);
        foreach (var prop in entry.Properties)
        {
            await _logService.ChangLog(
                logId,
                oldValue: "null",
                newValue: prop.CurrentValue?.ToString() ?? "null",
                columnName: prop.Metadata.Name
            );
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.ProjectId }, ProjectToDto(entity));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Project with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Project), entity.ProjectId);

        return Ok(ProjectToDto(entity));
    }


    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] ProjectEditDto projectEditDto)
    {
        var pr = await _context.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditDto.ProjectDtoId && !r.IsDeleted)
            ?? throw new KeyNotFoundException("Project Not Found");

        if (projectEditDto.ManagerId.HasValue)
        {
            var u = await _context.Users
                .Include(p => p.Role)
                .FirstOrDefaultAsync(r => r.UserId == projectEditDto.ManagerId)
                ?? throw new KeyNotFoundException("Manager Not Found");

            if (u.RoleId != Guid.Parse("6869DEFE-56C3-490A-BAB2-0EA92EC97BCA"))
                throw new InvalidOperationException("The role must be Manager");

            pr.ManagerId = projectEditDto.ManagerId.Value;
        }

        if (projectEditDto.PriorityId.HasValue)
        {
            var p = await _context.Priorities
                .FindAsync(projectEditDto.PriorityId)
                ?? throw new KeyNotFoundException("Priority Not Found");

            pr.PriorityId = projectEditDto.PriorityId;
        }

        if (!string.IsNullOrWhiteSpace(projectEditDto.Title))
        {
            var exists = await _context.Projects
                .AnyAsync(r => r.ProjectName == projectEditDto.Title && r.ProjectId != pr.ProjectId && !r.IsDeleted);

            if (exists)
                throw new InvalidOperationException("Project Name Exist");

            pr.ProjectName = projectEditDto.Title;
        }

        pr.Desc = projectEditDto.Description ?? pr.Desc;

        var entry = _context.Entry(pr);

        var modifiedProps = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => new
            {
                ColumnName = p.Metadata.Name,
                OldVal = p.OriginalValue?.ToString() ?? "null",
                NewVal = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        await _context.SaveChangesAsync();

        if (modifiedProps.Any())
        {
            var tableName = _context.Model
                .FindEntityType(typeof(Project))?
                .GetTableName() ?? nameof(Project);

            var logId = await _logService.Log("Edit", tableName, pr.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
        }

        return Ok(ProjectToDto(pr));
    }


    [HttpPatch("edit/date")]
    public async Task<IActionResult> EditDate([FromBody] EditAllDate editAllDate)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == editAllDate.ID && !p.IsDeleted)
            ?? throw new KeyNotFoundException("Project Not Found");

        var tm = await _context.TaskManagements
            .Where(t => t.ProjectId == entity.ProjectId)
            .ToListAsync();

        if (editAllDate.CreationDate.HasValue)
        {
            if (editAllDate.CreationDate > entity.StartDate || editAllDate.CreationDate > entity.EndDate)
                throw new InvalidOperationException("End/Start Date < Create Date");

            foreach (var e in tm)
            {
                if (e.CreationDate < editAllDate.CreationDate)
                    throw new InvalidOperationException("TM Creation Date < Project Creation Date");
            }
            entity.CreationDate = editAllDate.CreationDate.Value;
        }

        if (editAllDate.StartDate.HasValue)
        {
            if (editAllDate.StartDate > entity.EndDate || editAllDate.StartDate < entity.CreationDate)
                throw new InvalidOperationException("Start Date > End Date or Start Date < Creation Date");

            foreach (var e in tm)
            {
                if (editAllDate.StartDate > e.StartDate)
                    throw new InvalidOperationException("Start Date Project > Start Date Task");
            }

            entity.StartDate = editAllDate.StartDate.Value;
        }

        if (editAllDate.DueDate.HasValue)
        {
            if (editAllDate.DueDate > entity.EndDate || editAllDate.DueDate < entity.CreationDate)
                throw new InvalidOperationException("Due Date > End Date or Due Date < Creation Date");

            entity.DueDate = editAllDate.DueDate.Value;
        }

        if (editAllDate.EndDate.HasValue)
        {
            if (editAllDate.EndDate < entity.CreationDate || editAllDate.EndDate < entity.StartDate)
                throw new InvalidOperationException("End Date < Creation Date or End Date < Start Date");

            foreach (var e in tm)
                if (e.CompletionDate > editAllDate.EndDate)
                    throw new InvalidOperationException("Task Completion Date > Project Completion Date");

            entity.EndDate = editAllDate.EndDate.Value;
        }

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

        await _context.SaveChangesAsync();

        if (modifiedProps.Any())
        {
            var tableName = _context.Model
                .FindEntityType(typeof(Project))?
                .GetTableName() ?? nameof(Project);

            var logId = await _logService.Log("Edit", tableName, entity.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
        }

        return Ok(ProjectToDto(entity));
    }


    private ProjectDto ProjectToDto(Project project)
    {
        return new ProjectDto
        {
            ProjectId = project.ProjectId,
            StatusId = project.StatusId,
            ManagerId = project.ManagerId,
            PriorityId = project.PriorityId,
            ProjectName = project.ProjectName,
            Description = project.Desc,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreationDate = project.CreationDate,
            StatusName = project.Status?.StatusName,
            ManagerName = project.Manager?.UserFullName,
            PriorityName = project.Priority?.PriorityName
        };
    }
}
