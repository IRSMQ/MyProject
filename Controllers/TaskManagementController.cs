using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

namespace Test26.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TaskManagementController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public TaskManagementController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<TaskManagementDto>>> GetAll()
    {
        var tm = await _context.TaskManagements
            .Where(r => !((ISoftDeletable)r).IsDeleted)
            .Select(r => new TaskManagementDto
            {
                TaskManagementId = r.TaskManagementId,
                ProjectId = r.ProjectId,
                StatusId = r.StatusId,
                PriorityId = r.PriorityId,
                ParentId = r.ParentId,

                ProjectName = r.Project.ProjectName,
                StatusName = r.Status.StatusName,
                PriorityName = r.Priority.PriorityName,
                ParentName = r.Parent != null ? r.Parent.Title : null,

                Title = r.Title,
                Desc = r.Desc,
                CreationDate = r.CreationDate,
                StartDate = r.StartDate,
                DueDate = r.DueDate,
                CompletionDate = r.CompletionDate
            })
            .ToListAsync();

        if (tm.Count == 0)
            throw new InvalidOperationException("Task Managements Empity");

        return Ok(tm);
    }

    [HttpGet("byid/{id:guid}")]
    public async Task<ActionResult<TaskManagementDto>> GetById(Guid id)
    {
        var tm = await _context.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == id && !((ISoftDeletable)t).IsDeleted);

        if (tm == null)
            throw new KeyNotFoundException($"Task with ID {id} Not Found");

        return Ok(TaskToDto(tm));
    }

    [HttpPost("add")]
    public async Task<ActionResult<TaskManagementDto>> Add([FromBody] TMADto dto)
    {
        if (!dto.TaskId.HasValue)
        {
            var entity = await _context.TaskManagements
                .AnyAsync(t => t.Title == dto.Title && t.ProjectId == dto.ProjectId && t.ParentId == dto.ParentId && !((ISoftDeletable)t).IsDeleted);

            if (entity)
                throw new InvalidOperationException("Task Exist");

            if (dto.ParentId.HasValue)
            {
                var tm = await _context.TaskManagements
                    .FirstOrDefaultAsync(t => t.TaskManagementId == dto.ParentId && !((ISoftDeletable)t).IsDeleted)
                    ?? throw new KeyNotFoundException("Parent Not Found");

                dto.ProjectId = tm.ProjectId;
            }
            else
            {
                if (dto.ProjectId.HasValue)
                {
                    var tm = await _context.Projects
                        .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId)
                        ?? throw new KeyNotFoundException("Project Not Found");
                }
                else
                    throw new InvalidOperationException("The project cannot be empty");
            }

            if (!dto.CreationDate.HasValue)
                dto.CreationDate = DateTime.Now;

            if (dto.DueDate.HasValue)
                if (dto.CreationDate > dto.DueDate)
                    throw new InvalidOperationException("Creation Date > Due Date");

            if (dto.StartDate.HasValue)
                if (dto.CreationDate > dto.StartDate)
                    throw new InvalidOperationException("Creation Date > Start Date");

            if (dto.EndDate.HasValue && dto.StartDate.HasValue)
            {
                if (dto.StartDate.HasValue)
                    if (dto.StartDate > dto.EndDate)
                        throw new InvalidOperationException("Start Date > End Date");

                else
                    throw new InvalidOperationException("End Date Has Value And Start Date Empity");
            }   

            if (dto.StatusId.HasValue)
            {
                var st = _context.Statuses
                    .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId)
                    ?? throw new KeyNotFoundException("Status Not Found");
            }

            if (dto.PriorityId.HasValue)
            {
                var pr = _context.Priorities
                    .FirstOrDefaultAsync(p => p.PriorityId == dto.PriorityId)
                    ?? throw new KeyNotFoundException("Priority Not Found");
            }

            var taskId = NewTaskManagement(dto);

            var logId = await _logService.Log("Add",nameof(taskId), taskId);

            
        }
        else
        {
            
        }
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<ActionResult<TaskManagementDto>> Delete(Guid id)
    {
        var tm = await _context.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == id && !((ISoftDeletable)t).IsDeleted);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        var exist = await _context.TaskManagements
            .AnyAsync(r => r.ParentId == id);

        if (exist)
            throw new InvalidOperationException("This task has a sub-task");

        _context.TaskManagements.Remove(tm);
        await _context.SaveChangesAsync();

        return Ok(TaskToDto(tm));
    }

    [HttpPatch("edit/status")]
    public async Task<ActionResult<TaskManagementDto>> Status([FromBody] TaskEditObject taskEditObject)
    {
        var exist = await _context.Statuses
            .AnyAsync(t => t.StatusId == taskEditObject.ObjectId);

        if (!exist)
            throw new KeyNotFoundException("Status Not Found");

        var tm = await _context.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == taskEditObject.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (tm.Project != null && tm.Project.StatusId != null && tm.Project.StatusId == Guid.Parse("DB303C22-BB2F-438A-BDE0-401CFCA15B5C") && taskEditObject.ObjectId == Guid.Parse("8569E5B3-46BD-4070-BCC2-18AB1DC94F26"))
            throw new InvalidOperationException("When Project Complete, can't edit Task Status into InProgress");

        tm.StatusId = taskEditObject.ObjectId;

        await _context.SaveChangesAsync();

        return Ok(TaskToDto(tm));
    }

    [HttpPatch("edit/priority")]
    public async Task<ActionResult<TaskManagementDto>> Priority([FromBody] TaskEditObject taskEditObject)
    {
        var result = await _context.TaskManagements
            .FirstOrDefaultAsync(t => t.TaskManagementId == taskEditObject.TaskID && !((ISoftDeletable)t).IsDeleted);

        return Ok(result);
    }

    [HttpPatch("edit/parent")]
    public async Task<ActionResult<TaskManagementDto>> Parent([FromBody] TMEPDto tMEPDto)
    {
        var tm = await _context.TaskManagements
            .FirstOrDefaultAsync(t => t.TaskManagementId == tMEPDto.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (tMEPDto.ObjectId != null)
        {
            if (tMEPDto.TaskID == tMEPDto.ObjectId)
                throw new InvalidOperationException("Task cannot be its own Parent");

            var exist = await _context.TaskManagements
                .AnyAsync(t => t.TaskManagementId == tMEPDto.ObjectId);

            if (!exist)
                throw new KeyNotFoundException("Parent Not Found");

            var ptm = await _context.TaskManagements
                .FirstOrDefaultAsync(t => t.TaskManagementId == tMEPDto.ObjectId);

            if (ptm == null || tm.ProjectId != ptm.ProjectId)
                throw new InvalidOperationException("The Projects are not coordinated");

            var curentId = tMEPDto.ObjectId;
            var visited = new HashSet<Guid> { tMEPDto.TaskID };

            while (curentId != null)
            {
                if (!visited.Add(curentId.Value))
                    throw new InvalidOperationException("Circular reference detected");

                var parent = await _context.TaskManagements
                    .Where(t => t.TaskManagementId == curentId.Value)
                    .Select(t => t.ParentId)
                    .FirstOrDefaultAsync();

                if (parent == null)
                    break;

                if (parent == tMEPDto.TaskID)
                    throw new InvalidOperationException("Circular reference");

                curentId = parent;
            }
        }

        tm.ParentId = tMEPDto.ObjectId;

        await _context.SaveChangesAsync();

        return Ok(TaskToDto(tm));
    }

    [HttpPatch("edit/title")]
    public async Task<ActionResult<TaskManagementDto>> Title([FromBody] EditString editString)
    {
        var tm = await _context.TaskManagements
            .FindAsync(editString.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editString.Text == null)
            throw new InvalidOperationException("Title is Null");

        var tm2 = await _context.TaskManagements
            .Where(t => t.ProjectId == tm.ProjectId && t.ParentId == tm.ParentId)
            .ToListAsync();

        foreach (var t in tm2)
        {
            if (t.Title == editString.Text)
                throw new InvalidOperationException("This name is a duplicate within this project and task");
        }

        tm.Title = editString.Text;

        await _context.SaveChangesAsync();

        return Ok(TaskToDto(tm));
    }

    [HttpPatch("edit/desc")]
    public async Task<ActionResult<TaskManagementDto>> Desc([FromBody] EditString editString)
    {
        var tm = await _context.TaskManagements
            .FindAsync(editString.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        tm.Desc = editString.Text;

        await _context.SaveChangesAsync();

        return Ok(TaskToDto(tm));
    }

    [HttpPut("edit/date")]
    public async Task<ActionResult<TaskManagementDto>> EditDate([FromBody] EditDates editDate)
    {
        var entity = await _context.TaskManagements
            .FindAsync(editDate.ID);

        if (entity == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editDate.CreateDate.HasValue)
        {
            if (entity.StartDate < editDate.CreateDate || entity.DueDate < editDate.CreateDate || entity.CompletionDate < editDate.CreateDate)
                throw new InvalidOperationException("Creation/Start/Due Date > Date");

            var pr = await _context.Projects
                .FindAsync(entity.ProjectId);

            if (pr == null)
                throw new KeyNotFoundException("Project Not Found");

            if (pr.CreationDate > editDate.CreateDate)
                throw new InvalidOperationException("Project Creation Date > Task Creation Date");

            entity.CreationDate = editDate.CreateDate.Value;
        }

        if (editDate.StartDate.HasValue)
        {
            if (entity.DueDate < editDate.StartDate || entity.CompletionDate < editDate.StartDate || entity.CreationDate > editDate.StartDate)
                throw new InvalidOperationException("Completion/Due Date < Start Date or Creation Date > Start Date");

            var pr = await _context.Projects
                .FindAsync(entity.ProjectId)
                ?? throw new KeyNotFoundException("No Project was found for this Task");

            if (pr.StartDate > editDate.StartDate)
                throw new InvalidOperationException("Start Date Project > Start Date Task");

            entity.StartDate = editDate.StartDate.Value;
        }

        if (editDate.DueDate.HasValue)
        {
            if (entity.StartDate > editDate.DueDate || entity.CreationDate > editDate.DueDate)
                throw new InvalidOperationException("Creation/Start Date > Date");

            entity.DueDate = editDate.DueDate.Value;
        }

        if (editDate.EndDate.HasValue)
        {
            if (entity.CreationDate > editDate.EndDate || entity.StartDate > editDate.EndDate)
                throw new InvalidOperationException("Create/Start Date > End Date");

            var pr = await _context.Projects
                .FindAsync(entity.ProjectId);

            if (pr == null)
                throw new InvalidOperationException("Project Not Found");

            if (pr.EndDate < editDate.EndDate)
                throw new InvalidOperationException("TM Completion Date > Project Completion Date");

            entity.CompletionDate = editDate.EndDate;
        }

        var entiry = _context.TaskManagements.Entry(entity);

        var modifiedProps = entiry.Properties
            .Where(p => p.IsModified)
            .Select(p => new
            {
                ColumnName = entiry.Metadata.Name,
                OldVal = entiry.OriginalValues?.ToString() ?? "null",
                NewVal = entiry.CurrentValues?.ToString() ?? "null"
            })
            .ToList();

        if (modifiedProps.Any())
        {
            var tableName = _context.Model
                .FindEntityType(typeof(TaskManagement))?
                .GetTableName() ?? nameof(TaskManagement);

            var logid = await _logService.Log("Edit", tableName, entity.TaskManagementId);

            foreach (var m in modifiedProps)
                await _logService.ChangLog(logid, m.OldVal, m.NewVal, m.ColumnName);
        }
        
        await _context.SaveChangesAsync();

        return Ok(TaskToDto(entity));
    }

    private TaskManagementDto TaskToDto(TaskManagement taskManagement)
    {
        return new TaskManagementDto
        {
            TaskManagementId = taskManagement.TaskManagementId,
            ProjectId = taskManagement.ProjectId,
            StatusId = taskManagement.StatusId,
            PriorityId = taskManagement.PriorityId,
            ParentId = taskManagement.ParentId,

            ProjectName = taskManagement.Project?.ProjectName,
            StatusName = taskManagement.Status?.StatusName,
            PriorityName = taskManagement.Priority?.PriorityName,
            ParentName = taskManagement.Parent?.Title,

            Title = taskManagement.Title,
            Desc = taskManagement.Desc,
            CreationDate = taskManagement.CreationDate,
            StartDate = taskManagement.StartDate,
            DueDate = taskManagement.DueDate,
            CompletionDate = taskManagement.CompletionDate
        };
    }

    private Guid NewTaskManagement(TMADto dto)
    {
        var tkId = Guid.NewGuid();

        var newTask = new TaskManagement
        {
            TaskManagementId = tkId,
            ProjectId = dto.ProjectId.Value,
            StatusId = dto.StatusId,
            PriorityId = dto.PriorityId,
            ParentId = dto.ParentId,
            Title = dto.Title,
            Desc = dto.Desc,
            CreationDate = dto.CreationDate,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            CompletionDate = dto.EndDate
        };

        _context.TaskManagements.Add(newTask);

        return tkId;
    }
}
