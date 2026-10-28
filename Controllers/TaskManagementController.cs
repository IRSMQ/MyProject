using System.Linq.Expressions;
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
public class TaskManagementController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public TaskManagementController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .Where(e => !e.IsDeleted)
            .Select(e => new TaskManagementDto
            {
                TaskManagementId = e.TaskManagementId,
                ProjectId = e.ProjectId,
                StatusId = e.StatusId,
                PriorityId = e.PriorityId,
                ParentId = e.ParentId,

                ProjectName = e.Project.ProjectName,
                StatusName = e.Status.StatusName,
                PriorityName = e.Priority.PriorityName,
                ParentName = e.Parent != null ? e.Parent.Title : null,

                Title = e.Title,
                Desc = e.Desc,
                CreationDate = e.CreationDate,
                StartDate = e.StartDate,
                DueDate = e.DueDate,
                CompletionDate = e.CompletionDate
            })
            .ToListAsync();

        if (entity.Count == 0)
            throw new ArgumentException("Task Managements Empity");

        return Ok(ApiResponse<List<TaskManagementDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .FirstOrDefaultAsync(e => e.TaskManagementId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException($"Task Not Found");

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> Status([FromQuery] TaskEditObject dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditStatusTaskManagement");

        //////////////////////////////////////////////////////
        
        var exist = await _context.Statuses
            .AnyAsync(e => e.StatusId == dto.ObjectId && !e.IsDeleted);

        if (!exist)
            throw new KeyNotFoundException("Status Not Found");

        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        if (entity.Project != null && entity.Project.StatusId != null && entity.Project.StatusId == Guid.Parse("DB303C22-BB2F-438A-BDE0-401CFCA15B5C") && dto.ObjectId == Guid.Parse("8569E5B3-46BD-4070-BCC2-18AB1DC94F26"))
            throw new ArgumentException("When Project Complete, can't edit Task Status into InProgress");

        var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

        await _logService.ChangLog(
            logId,
            entity.Status?.ToString() ?? "null",
            dto.ObjectId?.ToString() ?? "null",
            nameof(entity.Desc)
        );

        entity.StatusId = dto.ObjectId;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Status Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> Priority([FromQuery] TaskEditObject dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditPriorityTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        var exist = await _context.Priorities
            .AnyAsync(e => e.PriorityId == dto.ObjectId);

        if (!exist)
            throw new KeyNotFoundException("Priority Not Found");

        var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

        await _logService.ChangLog(
            logId,
            entity.PriorityId?.ToString() ?? "null",
            dto.ObjectId?.ToString() ?? "null",
            nameof(entity.PriorityId)
        );

        entity.PriorityId = dto.ObjectId;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Priority Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> Parent([FromQuery] TMEPDto dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditParentTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(t => t.TaskManagementId == dto.TaskID)
            ?? throw new KeyNotFoundException("Task Not Found");

        if (dto.ObjectId != null)
        {
            if (dto.TaskID == dto.ObjectId)
                throw new ArgumentException("Task cannot be its own Parent");

            var exist = await _context.TaskManagements
                .AnyAsync(e => e.TaskManagementId == dto.ObjectId);

            if (!exist)
                throw new KeyNotFoundException("Parent Not Found");

            var ptm = await _context.TaskManagements
                .FirstOrDefaultAsync(e => e.TaskManagementId == dto.ObjectId);

            if (ptm == null || entity.ProjectId != ptm.ProjectId)
                throw new ArgumentException("The Projects are not coordinated");

            var curentId = dto.ObjectId;
            var visited = new HashSet<Guid> { dto.TaskID };

            while (curentId != null)
            {
                if (!visited.Add(curentId.Value))
                    throw new ArgumentException("Circular reference detected");

                var parent = await _context.TaskManagements
                    .Where(t => t.TaskManagementId == curentId.Value)
                    .Select(t => t.ParentId)
                    .FirstOrDefaultAsync();

                if (parent == null)
                    break;

                if (parent == dto.TaskID)
                    throw new ArgumentException("Circular reference");

                curentId = parent;
            }
        }

        var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

        await _logService.ChangLog(
            logId,
            entity.ParentId?.ToString() ?? "null",
            dto.ObjectId?.ToString() ?? "null",
            nameof(entity.ParentId)
        );

        entity.ParentId = dto.ObjectId;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Parent Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> Title([FromQuery] EditString editString)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditTitleTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == editString.TaskID && !e.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editString.Text == null)
            throw new ArgumentException("Title is Null");

        var tm2 = await _context.TaskManagements
            .Where(t => t.ProjectId == entity.ProjectId && t.ParentId == entity.ParentId)
            .ToListAsync();

        foreach (var t in tm2)
        {
            if (t.Title == editString.Text)
                throw new ArgumentException("This name is a duplicate within this project and task");
        }

        var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

        await _logService.ChangLog(
            logId,
            entity.Title?.ToString() ?? "null",
            editString.Text?.ToString() ?? "null",
            nameof(entity.Title)
        );

        entity.Title = editString.Text;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Title Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> Desc([FromQuery] EditString editString)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditDescTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == editString.TaskID && !e.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException("Task Not Found");

        var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

        await _logService.ChangLog(
            logId,
            entity.Desc?.ToString() ?? "null",
            editString.Text?.ToString() ?? "null",
            nameof(entity.Desc)
        );

        entity.Desc = editString.Text;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Desc Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> EditDate([FromQuery] EditDates editDate)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "EditDateTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == editDate.ID && !e.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editDate.CreateDate.HasValue)
        {
            if (entity.StartDate < editDate.CreateDate || entity.DueDate < editDate.CreateDate || entity.CompletionDate < editDate.CreateDate)
                throw new ArgumentException("Creation/Start/Due Date > Date");

            var pr = await _context.Projects
                .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted);

            if (pr == null)
                throw new KeyNotFoundException("Project Not Found");

            if (pr.CreationDate > editDate.CreateDate)
                throw new ArgumentException("Project Creation Date > Task Creation Date");

            entity.CreationDate = editDate.CreateDate.Value;
        }

        if (editDate.StartDate.HasValue)
        {
            if (entity.DueDate < editDate.StartDate || entity.CompletionDate < editDate.StartDate || entity.CreationDate > editDate.StartDate)
                throw new ArgumentException("Completion/Due Date < Start Date or Creation Date > Start Date");

            var pr = await _context.Projects
                .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("No Project was found for this Task");

            if (pr.StartDate > editDate.StartDate)
                throw new ArgumentException("Start Date Project > Start Date Task");

            entity.StartDate = editDate.StartDate.Value;
        }

        if (editDate.DueDate.HasValue)
        {
            if (entity.StartDate > editDate.DueDate || entity.CreationDate > editDate.DueDate)
                throw new ArgumentException("Creation/Start Date > Date");

            entity.DueDate = editDate.DueDate.Value;
        }

        if (editDate.EndDate.HasValue)
        {
            if (entity.CreationDate > editDate.EndDate || entity.StartDate > editDate.EndDate)
                throw new ArgumentException("Create/Start Date > End Date");

            var pr = await _context.Projects
                .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted);

            if (pr == null)
                throw new ArgumentException("Project Not Found");

            if (pr.EndDate < editDate.EndDate)
                throw new ArgumentException("TM Completion Date > Project Completion Date");

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

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Edit Date Succeeded"));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Add([FromQuery] TMDtoAdd dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        ////////////////////////////////////////////////////// 

        var exist = await _context.TaskManagements
            .AnyAsync(e => e.Title == dto.Title && e.ProjectId == dto.ProjectId && e.ParentId == dto.ParentId && !e.IsDeleted);

        if (exist)
            throw new ArgumentException("Task With this Title, Parent, Project Exist");

        var entity = new TaskManagement();
        

        if (!dto.TaskId.HasValue)
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "AddTaskManagement");
            if (dto.ParentId.HasValue)
            {
                var entityTaskManagement = await _context.TaskManagements
                    .FirstOrDefaultAsync(e => e.TaskManagementId == dto.ParentId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("Parent Not Found");

                dto.ProjectId = entityTaskManagement.ProjectId;
            }
            else
            {
                if (dto.ProjectId.HasValue)
                {
                    var entityProject = await _context.Projects
                        .FirstOrDefaultAsync(e => e.ProjectId == dto.ProjectId)
                        ?? throw new KeyNotFoundException("Project Not Found");
                }
                else
                    throw new ArgumentException("The project cannot be empty");
            }

            if (!dto.CreationDate.HasValue)
                dto.CreationDate = DateTime.Now;

            if (dto.EndDate.HasValue)
            {
                if (dto.StartDate.HasValue)
                    if (dto.StartDate > dto.EndDate)
                        throw new ArgumentException("Start Date > End Date");

                else
                    throw new ArgumentException("End Date Has Value And Start Date Empity");
                
                if (dto.CreationDate.HasValue)
                    if (dto.CreationDate > dto.EndDate)
                        throw new ArgumentException("Createion Date > End Date");

                if (dto.DueDate.HasValue)
                    if (dto.DueDate > dto.EndDate)
                        throw new ArgumentException("Due Date > End Date");
            }

            if (dto.DueDate.HasValue)
                if (dto.CreationDate > dto.DueDate)
                    throw new ArgumentException("Creation Date > Due Date");

            if (dto.StartDate.HasValue)
                if (dto.CreationDate > dto.StartDate)
                    throw new ArgumentException("Creation Date > Start Date");

            if (dto.StatusId.HasValue)
            {
                var statusEntity = await _context.Statuses
                    .FirstOrDefaultAsync(p => p.StatusId == dto.StatusId)
                    ?? throw new KeyNotFoundException("Status Not Found");
            }

            if (dto.PriorityId.HasValue)
            {
                var entityPriority = await _context.Priorities
                    .FirstOrDefaultAsync(p => p.PriorityId == dto.PriorityId)
                    ?? throw new KeyNotFoundException("Priority Not Found");
            }

            Guid taskId = Guid.NewGuid();
            var newTask = new TaskManagement
        {
            TaskManagementId = taskId,
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

            await _context.TaskManagements.AddAsync(newTask);

            var logId = await _logService.Log("Create",nameof(TaskManagement), taskId);

            var entry = _context.Entry(newTask);

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

            entity = newTask;
        }
        else
        {
            await _logService.CheckToken(tokenUserId, tokenRoleId, "EditTaskManagement");
            entity = await _context.TaskManagements
                .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Task Not Found");

            if (dto.Nullable)
            {
                entity.StatusId = dto.StatusId;
                entity.PriorityId = dto.PriorityId;
                entity.ParentId = dto.ParentId;

                entity.Desc = dto.Desc;
                entity.CreationDate = dto.CreationDate;
                entity.CompletionDate = dto.EndDate;

                entity.DueDate = dto.DueDate;
                entity.StartDate = dto.StartDate;
            }

            

            if (dto.ParentId.HasValue)
            {
                var entityTaskManagement = await _context.TaskManagements
                    .FirstOrDefaultAsync(e => e.TaskManagementId == dto.ParentId && !e.IsDeleted)
                    ?? throw new KeyNotFoundException("Parent Not Found");

                entity.ProjectId = entityTaskManagement.ProjectId;
                entity.ParentId = dto.ParentId;
            }
            else
            {
                if (dto.ProjectId.HasValue)
                {
                    var entityProject = await _context.Projects
                        .FirstOrDefaultAsync(e => e.ProjectId == dto.ProjectId)
                        ?? throw new KeyNotFoundException("Project Not Found");
                    
                    entity.ProjectId = dto.ProjectId.Value;
                }
                else
                    throw new ArgumentException("The project cannot be empty");
            }

            if (dto.CreationDate.HasValue)
                entity.CreationDate = dto.CreationDate;

            if (dto.EndDate.HasValue)
            {
                if (entity.CreationDate > dto.EndDate)
                    throw new ArgumentException("Createion Date > End Date");

                entity.CompletionDate = dto.EndDate;
            }

            if (dto.StartDate.HasValue)
            {
                if (entity.CreationDate > dto.StartDate || entity.CompletionDate < dto.StartDate)
                    throw new ArgumentException("Creation Date > Start Date or End Date < Start Date");

                entity.StartDate = dto.StartDate;
            }

            if (dto.DueDate.HasValue)
            {
                if (entity.CreationDate > dto.DueDate)
                    throw new ArgumentException("Creation Date > Due Date");

                entity.DueDate = dto.DueDate;
            }

            if (dto.StatusId.HasValue)
            {
                var entityStatus = await _context.Statuses
                    .FirstOrDefaultAsync(e => e.StatusId == dto.StatusId)
                    ?? throw new KeyNotFoundException("Status Not Found");

                entity.StatusId = dto.StatusId;
            }

            if (dto.PriorityId.HasValue)
            {
                var entityPriority = await _context.Priorities
                    .FirstOrDefaultAsync(p => p.PriorityId == dto.PriorityId)
                    ?? throw new KeyNotFoundException("Priority Not Found");

                entity.PriorityId = dto.PriorityId;
            }

            if (dto.Title != null)
            {
                entity.Title = dto.Title;
            }

            var logId = await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId);

            var entiry = _context.Entry(entity);

            var modifiedProps = entiry.Properties
                .Where(e => e.IsModified)
                .Select(e => new
                {
                    ColumnName = e.Metadata.Name,
                    OldVal = e.OriginalValue?.ToString() ?? "null",
                    NewVal = e.CurrentValue?.ToString() ?? "null"
                })
                .ToList();

            foreach (var prop in modifiedProps)
            {
                await _logService.ChangLog(
                    logId,
                    prop.OldVal,
                    prop.NewVal,
                    prop.ColumnName
                );
            }
        }

        var newEntity = TaskToDto(entity);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(newEntity,$"Succeeded"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteTaskManagement");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .FirstOrDefaultAsync(e => e.TaskManagementId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        var exist = await _context.TaskManagements
            .AnyAsync(e => e.ParentId == id);

        if (exist)
            throw new ArgumentException("This task has a sub-task");

        await _logService.Log("Delete", nameof(TaskManagement), entity.TaskManagementId);

        entity.IsDeleted = true;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(TaskToDto(entity),$"Delete Succeeded"));
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

}