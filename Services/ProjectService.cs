using Test26.Exception;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Differencing;
using System.Linq.Expressions;
using System.Data;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Test26.EventS;

namespace Test26.Service;

public interface IProjectService
{
    
}


public class ProjectService : IProjectService
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    public ProjectService(ProjectManagementSystemContext projectManagementSystemContext, LogService logService)
    {
        _context = projectManagementSystemContext;
        _logService = logService;
    }

    public async Task<List<ProjectDto>> GetAll()
    {
        return await _context.Projects
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
    }

    public async Task<ProjectDto> GetById(Guid id)
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

        return pm;
    }


    public async Task<ProjectDto> Add(CreateProject dto)
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
            await _logService.ChangLog(
                logId,
                oldValue: "null",
                newValue: prop.CurrentValue?.ToString() ?? "null",
                columnName: prop.Metadata.Name
            );
        }

        await _context.SaveChangesAsync();

        return ProjectToDto(entity);
    }


    public async Task<ProjectDto> DeleteAsync(Guid id)
    {
        var entity = await _context.Projects
            .FirstOrDefaultAsync(p => p.ProjectId == id && !p.IsDeleted);

        if (entity == null)
            throw new KeyNotFoundException($"Project with ID {id} not found.");

        entity.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _logService.Log("Delete", nameof(Project), entity.ProjectId);

        return ProjectToDto(entity);
    }

    /*
    public async Task<ProjectDto> EditP(ProjectEditDto projectEditDto)
    {
        var pr = await _projectManagementSystemContext.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditDto.ProjectDtoId)
            ?? throw new KeyNotFoundException("Project Not Found");

        
        var u = await _projectManagementSystemContext.Users
            .Include(p => p.Role)
            .FirstOrDefaultAsync(r => r.UserId == projectEditDto.ManagerId)
            ?? throw new KeyNotFoundException("Manager Not Found");

        pr.StatusId = projectEditDto.StatusId ?? pr.StatusId;
        
        if (projectEditDto.ManagerId != null)
        {
            pr.ManagerId = u.RoleId == Guid.Parse("6869DEFE-56C3-490A-BAB2-0EA92EC97BCA")
                ? projectEditDto.ManagerId.Value
                : throw new InvalidOperationException("The role must be Manager");
        }

        pr.PriorityId = projectEditDto.PriorityId ?? pr.PriorityId;
        pr.ProjectName = projectEditDto.Title ?? pr.ProjectName;
        pr.Desc = projectEditDto.Description ?? pr.Desc;
        pr.StartDate = projectEditDto.StartDate ?? pr.StartDate;
        pr.EndDate = projectEditDto.EndDate ?? pr.EndDate;
        pr.CreationDate = projectEditDto.CreationDate ?? pr.CreationDate;

        await _projectManagementSystemContext.SaveChangesAsync();

        var tablename = _projectManagementSystemContext.Model
            .FindEntityType(typeof(Project))?
            .GetTableName() ?? typeof(Project).Name;

        await LogRegister("Edit",tablename,pr.ProjectId);

        return ProjectToDto(pr);
    }
    */

    public async Task<ProjectDto> EditP(ProjectEditDto projectEditDto)
    {
        var pr = await _context.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditDto.ProjectDtoId)
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

        if (projectEditDto.Title != null && projectEditDto.Title != "")
        {
            var p = await _context.Projects
                .AnyAsync(r => r.ProjectName == projectEditDto.Title);

            if (p)
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

            var logId = await _logService.Log("Edit",tableName,pr.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
            
            await _context.SaveChangesAsync();
        }
        return ProjectToDto(pr);
    }
    public async Task<ProjectDto> EditDate(EditAllDate editAllDate)
    {
        var entity = await _context.Projects
            .FindAsync(editAllDate.ID)
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

            var logId = await _logService.Log("Edit",tableName,entity.ProjectId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
            
            await _context.SaveChangesAsync();
        }
        return ProjectToDto(entity);
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
    private async Task LogRegister(string logtypename ,string tablename, Guid r)
    {
        await _logService.Log
        (
            logTypeName: logtypename,
            tableName: tablename,
            relatedId: r
        );
    }



    /*
    public async Task<ProjectDto> EditManager(EditManager editManager)
    {
        var ur = await _projectManagementSystemContext.Users
            .Include(r => r.Role)
            .FirstOrDefaultAsync(u => u.UserId == editManager.managerId)
        ??
        throw new KeyNotFoundException("Manager Not Found");

        if (ur.RoleId == null || ur.RoleId != Guid.Parse("6869DEFE-56C3-490A-BAB2-0EA92EC97BCA"))
            throw new InvalidOperationException("The user must only be an Manager");

        var pr = await _projectManagementSystemContext.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == editManager.projectId)
        ??
        throw new KeyNotFoundException("Project Not Found");

        pr.ManagerId = editManager.managerId;

        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, Manager",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    public async Task<ProjectDto> EditStatus(ProjectEditStatus projectEditStatus)
    {
        var pr = await _projectManagementSystemContext.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditStatus.ProjectId)
            ?? throw new KeyNotFoundException("Project Not Found");

        var st = await _projectManagementSystemContext.Statuses.FindAsync(projectEditStatus.StatusId)
        ?? throw new KeyNotFoundException("Status Not Found");

        if (projectEditStatus.StatusId == Guid.Parse("DB303C22-BB2F-438A-BDE0-401CFCA15B5C"))
        {
            var tm = await _projectManagementSystemContext.TaskManagements
                .Where(t => t.ProjectId == projectEditStatus.ProjectId)
                .ToListAsync();

            foreach (var e in tm)
            {
                if (e.StatusId == Guid.Parse("8569E5B3-46BD-4070-BCC2-18AB1DC94F26"))
                e.StatusId = Guid.Parse("3B96A6F6-63B3-452C-930A-1C76E125CDFF");
            }
        }

        pr.StatusId = projectEditStatus.StatusId;

        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, Status",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    public async Task<ProjectDto> EditPriority(ProjectEditStatus projectEditStatus)
    {
        var pr = await _projectManagementSystemContext.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditStatus.ProjectId);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");

        var st = await _projectManagementSystemContext.Priorities.FindAsync(projectEditStatus.StatusId);

        if (st == null)
            throw new KeyNotFoundException("Priority Not Found");

        pr.PriorityId = projectEditStatus.StatusId;

        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, Priority",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    */


    /*
    public async Task<ProjectDto> EditCreationDate(EditDate editDate)
    {
        if (editDate == null)
            throw new InvalidOperationException("Edit Date is Null");

        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID)
        ?? throw new KeyNotFoundException("Project Not Found");

        if (editDate.Date == null)
            throw new InvalidOperationException("Date is Null");

        if (editDate.Date > pr.StartDate || editDate.Date > pr.EndDate)
            throw new InvalidOperationException("End/Start Date < Create Date");

        var tm = await _projectManagementSystemContext.TaskManagements
            .Where(t => t.ProjectId == pr.ProjectId)
            .ToListAsync();

        foreach (var e in tm)
        {
            if (e.CreationDate < editDate.Date)
                throw new InvalidOperationException("TM Creation Date < Project Creation Date");
        }

        pr.CreationDate = editDate.Date.Value;
        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, Creation Date",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    */


    /*
    public async Task<ProjectDto> EditStartDate(EditDate editDate)
    {
        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID)
            ?? throw new KeyNotFoundException("Project Not Found");

        if (editDate.Date > pr.EndDate || editDate.Date < pr.CreationDate)
            throw new InvalidOperationException("Start Date > End Date or Start Date < Creation Date");

        var tm = await _projectManagementSystemContext.TaskManagements
            .Where(t => t.ProjectId == editDate.ID)
            .ToListAsync();

        foreach (var e in tm)
        {
            if (editDate.Date > e.StartDate)
                throw new InvalidOperationException("Start Date Project > Start Date Task");
        }


        pr.StartDate = editDate.Date;
        await _projectManagementSystemContext.SaveChangesAsync();
        
        await EventRegistration("Edit in Project, Start Date",relatedIdG:pr.ProjectId);
        
        return ProjectToDto(pr);
    }
    */


    /*
    public async Task<ProjectDto> EditDueDate(EditDate editDate)
    {
        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");

        if (editDate.Date == null)
            throw new InvalidOperationException("Date is Null");

        if (editDate.Date > pr.EndDate || editDate.Date < pr.CreationDate)
            throw new InvalidOperationException("Due Date > End Date or Due Date < Creation Date");

        pr.DueDate = editDate.Date.Value;
        await _projectManagementSystemContext.SaveChangesAsync();
        
        await EventRegistration("Edit in Project, Due Date",relatedIdG:pr.ProjectId);
        
        return ProjectToDto(pr);
    }
    */


    /*
    public async Task<ProjectDto> EditEndDate(EditDate editDate)
    {
        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");

        if (editDate == null)
            throw new InvalidOperationException("Date is Null");

        var tm = await _projectManagementSystemContext.TaskManagements
            .Where(t => t.ProjectId == editDate.ID)
            .ToListAsync();

        foreach (var e in tm)
        {
            if (e.CompletionDate > editDate.Date)
                throw new InvalidOperationException("Task Completion Date > Project Completion Date");
        }

        pr.EndDate = editDate.Date;
        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, End Date",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    */
}