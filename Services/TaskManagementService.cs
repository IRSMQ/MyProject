using System.Linq.Expressions;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;

namespace Test26.Service;

public class TaskManagementService
{
    private readonly ProjectManagementSystemContext _projectManagementSystemContext;
    public TaskManagementService(ProjectManagementSystemContext projectManagementSystemContext)
    {
        _projectManagementSystemContext = projectManagementSystemContext;
    }

    public async Task<List<TaskManagementDto>> GetAll()
    {
        var tm = await _projectManagementSystemContext.TaskManagements
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

        return tm;
    }

    public async Task<TaskManagementDto> GetById(Guid id)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == id && !((ISoftDeletable)t).IsDeleted);

        if (tm == null)
            throw new KeyNotFoundException($"Task with ID {id} Not Found");

        return TaskToDto(tm);
    }

    public async Task<TaskManagementDto> Create(TMADto tMADto)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .AnyAsync(t => t.Title == tMADto.Title && t.ProjectId == tMADto.ProjectId && t.ParentId == tMADto.ParentId);

        if (tm)
            throw new InvalidOperationException("Task Exist");

        if (tMADto.ParentId != null)
        {
            var tm2 = await _projectManagementSystemContext.TaskManagements
                .FirstOrDefaultAsync(t => t.TaskManagementId == tMADto.ParentId);
            
            if (tm2 == null)
                throw new InvalidOperationException("Parent Not Found");

            if (tm2.ProjectId != tMADto.ProjectId)
                throw new InvalidOperationException("The Projects are not coordinated");
        }

        if (tMADto.CreationDate != null && tMADto.DueDate != null)
        {
            if (tMADto.CreationDate > tMADto.DueDate)
                throw new InvalidOperationException("Creation Date > Due Date");
        }

        if (tMADto.CreationDate != null && tMADto.StartDate != null)
        {
            if (tMADto.CreationDate > tMADto.StartDate)
                throw new InvalidOperationException("Creation Date > Start Date");
        }

        var newTm = new TaskManagement
        {
            ProjectId = tMADto.ProjectId,
            StatusId = tMADto.StatusId,
            PriorityId = tMADto.PriorityId,
            ParentId = tMADto.ParentId,
            Title = tMADto.Title,
            Desc = tMADto.Desc,
            CreationDate = tMADto.CreationDate ?? DateTime.Now,
            StartDate = tMADto.StartDate ?? DateTime.Now,
            DueDate = tMADto.DueDate
        };

        _projectManagementSystemContext.TaskManagements.Add(newTm);
        await _projectManagementSystemContext.SaveChangesAsync();

        var saved = await _projectManagementSystemContext.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstAsync(t => t.TaskManagementId == newTm.TaskManagementId);

        return TaskToDto(saved);
    }

    public async Task<TaskManagementDto> Delete(Guid id)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == id && !((ISoftDeletable)t).IsDeleted);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        var exist = await _projectManagementSystemContext.TaskManagements
            .AnyAsync(r => r.ParentId == id);

        if (exist)
            throw new InvalidOperationException("This task has a sub-task");

        _projectManagementSystemContext.TaskManagements.Remove(tm);
        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }

    public async Task<TaskManagementDto> Status(TaskEditObject taskEditObject)
    {
        var exist = await _projectManagementSystemContext.Statuses
            .AnyAsync(t => t.StatusId == taskEditObject.ObjectId);

        if (!exist)
            throw new KeyNotFoundException("Status Not Found");

        var tm = await _projectManagementSystemContext.TaskManagements
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

        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }

    public async Task<TaskManagementDto> Priority(TaskEditObject taskEditObject)
    {
        return await Editing<Priority>(
            taskEditObject,
            p => p.PriorityId == taskEditObject.ObjectId,
            tm => tm.PriorityId = taskEditObject.ObjectId,
            "Priority Not Found"
        );
    }

    public async Task<TaskManagementDto> Parent(TMEPDto tMEPDto)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FirstOrDefaultAsync(t => t.TaskManagementId == tMEPDto.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (tMEPDto.ObjectId != null)
        {
            if (tMEPDto.TaskID == tMEPDto.ObjectId)
                throw new InvalidOperationException("Task cannot be its own Parent");

            var exist = await _projectManagementSystemContext.TaskManagements
            .AnyAsync(t => t.TaskManagementId == tMEPDto.ObjectId);

            if (!exist)
                throw new KeyNotFoundException("Parent Not Found");

            var ptm = await _projectManagementSystemContext.TaskManagements
                .FirstOrDefaultAsync(t => t.TaskManagementId == tMEPDto.ObjectId);

            if (ptm == null || tm.ProjectId != ptm.ProjectId)
                throw new InvalidOperationException("The Projects are not coordinated");

            var curentId = tMEPDto.ObjectId;
            var visited = new HashSet<Guid> { tMEPDto.TaskID };

            while (curentId != null)
            {
                if (!visited.Add(curentId.Value))
                    throw new InvalidOperationException("Circular reference detected");

                var parent = await _projectManagementSystemContext.TaskManagements
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

        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }

    public  async Task<TaskManagementDto> Title(EditString editString)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editString.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editString.Text == null)
            throw new InvalidOperationException("Title is Null");

        var tm2 = await _projectManagementSystemContext.TaskManagements
            .Where(t => t.ProjectId == tm.ProjectId && t.ParentId == tm.ParentId)
            .ToListAsync();

        foreach (var t in tm2)
        {
            if (t.Title == editString.Text)
                throw new InvalidOperationException("This name is a duplicate within this project and task");
        }

        tm.Title = editString.Text;

        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }

    public  async Task<TaskManagementDto> Desc(EditString editString)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editString.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        tm.Desc = editString.Text;

        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }


    public async Task<TaskManagementDto> CreateDate(EditDate editDate)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editDate.ID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editDate.Date == null)
            throw new InvalidOperationException("The Creation Date must not be empty");

        if (tm.StartDate < editDate.Date || tm.DueDate < editDate.Date || tm.CompletionDate < editDate.Date)
            throw new InvalidOperationException("Creation/Start/Due Date > Date");

        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(tm.ProjectId);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");
            
        if (pr.CreationDate > editDate.Date)
            throw new InvalidOperationException("Project Creation Date > Task Creation Date");

        tm.CreationDate = editDate.Date.Value;
        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }
    public async Task<TaskManagementDto> StartDate(EditDate editDate)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editDate.ID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (editDate.Date == null)
            throw new InvalidOperationException("The Start Date must not be empty");

        if (tm.DueDate < editDate.Date || tm.CompletionDate < editDate.Date || tm.CreationDate > editDate.Date)
            throw new InvalidOperationException("Completion/Due Date > Date or Creation < Date");

        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(tm.ProjectId);

        if (pr == null || pr.StartDate > editDate.Date)
            throw new InvalidOperationException("Start Date Project > Start Date Task");

        tm.StartDate = editDate.Date.Value;
        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }
    public async Task<TaskManagementDto> DueDate(EditDate editDate)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editDate.ID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (tm.StartDate > editDate.Date || tm.CompletionDate < editDate.Date)
            throw new InvalidOperationException("Creation/Start Date > Date");

        tm.DueDate = editDate.Date;
        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }
    public async Task<TaskManagementDto> CompletionDate(EditDate editDate)
    {
        var tm = await _projectManagementSystemContext.TaskManagements
            .FindAsync(editDate.ID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        if (tm.CreationDate > editDate.Date || tm.StartDate > editDate.Date)
            throw new InvalidOperationException("Create/Start Date > Date");

        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(tm.ProjectId);

        if (pr == null)
            throw new InvalidOperationException("Project Not Found");

        if (pr.EndDate < editDate.Date)
            throw new InvalidOperationException("TM Completion Date < Project Completion Date");

        tm.CompletionDate = editDate.Date;
        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
    }


    private async Task<TaskManagementDto> Editing<T>(
        TaskEditObject taskEditObject,
        Expression<Func<T,bool>> existsPredicate,
        Action<TaskManagement> updateAction,
        string entityNotFoundMessage) where T : class
    {
        var exist = await _projectManagementSystemContext
            .Set<T>()
            .AnyAsync(existsPredicate);

        if (!exist)
            throw new KeyNotFoundException(entityNotFoundMessage);

        var tm = await _projectManagementSystemContext.TaskManagements
            .Include(r => r.Project)
            .Include(r => r.Status)
            .Include(r => r.Priority)
            .Include(r => r.Parent)
            .FirstOrDefaultAsync(t => t.TaskManagementId == taskEditObject.TaskID);

        if (tm == null)
            throw new KeyNotFoundException("Task Not Found");

        updateAction(tm);

        await _projectManagementSystemContext.SaveChangesAsync();

        return TaskToDto(tm);
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