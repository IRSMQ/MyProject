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
    private readonly IAuthorizationService _authorizationService;

    public TaskManagementController(
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
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            // .Include(e => e.TaskDocuments)
            //     .ThenInclude(e => e.Document)
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
                CompletionDate = e.CompletionDate,

                Documents = e.TaskDocuments
                    .Where(t => t.Task.TaskManagementId == e.TaskManagementId)
                    .Select(t => new DocumentDto
                    {
                        DocumentId = t.Document.DocumentId,
                        FileName = t.Document.FileName
                    }).ToList(),

                Users = e.TaskUsers
                    .Where(t => t.Task.TaskManagementId == e.TaskManagementId)
                    .Select(t => new UserDto
                    {
                        UserId = t.User.UserId,
                        UserFullName = t.User.UserFullName
                    }).ToList()
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

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] TMDtoAdd dto)
    {
        var entity = new TaskManagement();

        string message = string.Empty;
        string textLog = string.Empty;

        if (dto.TaskId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskManagement.Edit")).Succeeded)
                return Forbid();

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
                    entity.ProjectId = dto.ProjectId.Value;
                }
                else
                    throw new ArgumentException("The project cannot be empty");
            }

            if (dto.CreationDate.HasValue)
                entity.CreationDate = dto.CreationDate;

            if (dto.EndDate.HasValue)
                entity.CompletionDate = dto.EndDate;

            if (dto.StartDate.HasValue)
                entity.StartDate = dto.StartDate;

            if (dto.DueDate.HasValue)
                entity.DueDate = dto.DueDate;

            if (dto.StatusId.HasValue)
                entity.StatusId = dto.StatusId;

            if (dto.PriorityId.HasValue)
                entity.PriorityId = dto.PriorityId;

            if (dto.Title != null)
                entity.Title = dto.Title;

            message = "Edit Succeeded";
            textLog = "Edit";
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskManagement.Create")).Succeeded)
                return Forbid();
            
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
                    entity.ProjectId = dto.ProjectId.Value;
                else
                    throw new ArgumentException("The project cannot be empty");
            }

            if (!dto.CreationDate.HasValue)
                dto.CreationDate = DateTime.Now;

            if (dto.EndDate.HasValue)
                entity.CompletionDate = dto.EndDate;

            if (dto.DueDate.HasValue)
                entity.DueDate = dto.DueDate;

            if (dto.StartDate.HasValue)
                entity.StartDate = dto.StartDate;

            if (dto.StatusId.HasValue)
                entity.StatusId = dto.StatusId;

            if (dto.PriorityId.HasValue)
                entity.PriorityId = dto.PriorityId;

            if (!string.IsNullOrEmpty(dto.Title))
                entity.Title = dto.Title;
            else
                throw new ArgumentException("Title Cannot Be Empty");

            if (!string.IsNullOrEmpty(dto.Desc))
                entity.Desc = dto.Desc;

            entity.TaskManagementId = Guid.NewGuid();

            await _context.TaskManagements.AddAsync(entity);

            message = "Create Succeeded";
            textLog = "Create";
        }

        var entry = _context.Entry(entity);

        await _logService.Log(textLog, nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"{message}"));
    }

    [Authorize(Policy = "TaskManagement.Status.Edit")]
    [HttpPatch]
    public async Task<IActionResult> Status([FromBody] TaskEditObject dto)
    {
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

        entity.StatusId = dto.ObjectId;

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"Edit Status Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Priority.Edit")]
    [HttpPatch]
    public async Task<IActionResult> Priority([FromBody] TaskEditObject dto)
    {
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        var exist = await _context.Priorities
            .AnyAsync(e => e.PriorityId == dto.ObjectId);

        if (!exist)
            throw new KeyNotFoundException("Priority Not Found");

        entity.PriorityId = dto.ObjectId;

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"Edit Priority Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Parent.Edit")]
    [HttpPatch]
    public async Task<IActionResult> Parent([FromBody] TMEPDto dto)
    {
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
                    .Where(e => e.TaskManagementId == curentId.Value)
                    .Select(e => e.ParentId)
                    .FirstOrDefaultAsync();

                if (parent == null)
                    break;

                if (parent == dto.TaskID)
                    throw new ArgumentException("Circular reference");

                curentId = parent;
            }
        }

        entity.ParentId = dto.ObjectId;

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"Edit Parent Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Title.Edit")]
    [HttpPatch]
    public async Task<IActionResult> Title([FromBody] EditString editString)
    {
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

        entity.Title = editString.Text;

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId),$"Edit Title Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Desc.Edit")]
    [HttpPatch]
    public async Task<IActionResult> Desc([FromBody] EditString editString)
    {
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == editString.TaskID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        entity.Desc = editString.Text;

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId),$"Edit Desc Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Date.Edit")]
    [HttpPut]
    public async Task<IActionResult> EditDate([FromBody] EditDates dto)
    {
        var entity = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.ID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        // if (editDate.CreateDate.HasValue)
        // {
        //     if (entity.StartDate < editDate.CreateDate || entity.DueDate < editDate.CreateDate || entity.CompletionDate < editDate.CreateDate)
        //         throw new ArgumentException("Creation/Start/Due Date > Date");

        //     var pr = await _context.Projects
        //         .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted);

        //     if (pr == null)
        //         throw new KeyNotFoundException("Project Not Found");

        //     if (pr.CreationDate > editDate.CreateDate)
        //         throw new ArgumentException("Project Creation Date > Task Creation Date");

        //     entity.CreationDate = editDate.CreateDate.Value;
        // }

        // if (editDate.StartDate.HasValue)
        // {
        //     if (entity.DueDate < editDate.StartDate || entity.CompletionDate < editDate.StartDate || entity.CreationDate > editDate.StartDate)
        //         throw new ArgumentException("Completion/Due Date < Start Date or Creation Date > Start Date");

        //     var pr = await _context.Projects
        //         .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted)
        //         ?? throw new KeyNotFoundException("No Project was found for this Task");

        //     if (pr.StartDate > editDate.StartDate)
        //         throw new ArgumentException("Start Date Project > Start Date Task");

        //     entity.StartDate = editDate.StartDate.Value;
        // }

        // if (editDate.DueDate.HasValue)
        // {
        //     if (entity.StartDate > editDate.DueDate || entity.CreationDate > editDate.DueDate)
        //         throw new ArgumentException("Creation/Start Date > Date");

        //     entity.DueDate = editDate.DueDate.Value;
        // }

        // if (editDate.EndDate.HasValue)
        // {
        //     if (entity.CreationDate > editDate.EndDate || entity.StartDate > editDate.EndDate)
        //         throw new ArgumentException("Create/Start Date > End Date");

        //     var pr = await _context.Projects
        //         .FirstOrDefaultAsync(e => e.ProjectId == entity.ProjectId && !e.IsDeleted);

        //     if (pr == null)
        //         throw new ArgumentException("Project Not Found");

        //     if (pr.EndDate < editDate.EndDate)
        //         throw new ArgumentException("TM Completion Date > Project Completion Date");

        //     entity.CompletionDate = editDate.EndDate;
        // }

        if (dto.CreateDate.HasValue)
            entity.CreationDate = dto.CreateDate;

        if (dto.StartDate.HasValue)
            entity.StartDate = dto.StartDate;

        if (dto.DueDate.HasValue)
            entity.DueDate = dto.DueDate;

        if (dto.EndDate.HasValue)
            entity.CompletionDate = dto.EndDate;

        var entiry = _context.TaskManagements.Entry(entity);

        await _logService.Log("Edit", nameof(TaskManagement), entity.TaskManagementId, entiry);
        
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId),$"Edit Date Succeeded"));
    }

    [Authorize(Policy = "TaskManagement.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteID dto)
    {
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.ID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        var exist = await _context.TaskManagements
            .AnyAsync(e => e.ParentId == dto.ID);

        if (exist)
            throw new ArgumentException("This task has a sub-task");

        await _logService.Log("Delete", nameof(TaskManagement), entity.TaskManagementId);

        entity.IsDeleted = true;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskManagementDto>.Success(await TaskToDto(entity.TaskManagementId), $"Delete Succeeded"));
    }


    private async Task<TaskManagementDto> TaskToDto(Guid id)
    {
        var entity = await _context.TaskManagements
            .Include(e => e.Project)
            .Include(e => e.Status)
            .Include(e => e.Priority)
            .Include(e => e.Parent)
            .FirstOrDefaultAsync(e => e.TaskManagementId == id);
            
        return new TaskManagementDto
        {
            TaskManagementId = entity.TaskManagementId,
            ProjectId = entity.ProjectId,
            StatusId = entity.StatusId,
            PriorityId = entity.PriorityId,
            ParentId = entity.ParentId,

            ProjectName = entity.Project.ProjectName,
            StatusName = entity.Status?.StatusName,
            PriorityName = entity.Priority?.PriorityName,
            ParentName = entity.Parent?.Title,

            Title = entity.Title,
            Desc = entity.Desc,
            CreationDate = entity.CreationDate,
            StartDate = entity.StartDate,
            DueDate = entity.DueDate,
            CompletionDate = entity.CompletionDate
        };
    }

}