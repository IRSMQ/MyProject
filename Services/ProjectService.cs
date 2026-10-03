using Test26.Exception;
using Test26.Data;
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


public class ProjectService :PublicService<Project,ProjectDto,CreateProject>, IProjectService
{
    public ProjectService(ProjectManagementSystemContext projectManagementSystemContext, EventService eventService)
    : base(projectManagementSystemContext,eventService)
    {
    }

    protected override Guid GetId(Project p) => p.ProjectId;
    protected override Guid GetDtoId(ProjectDto dto) => dto.ProjectId;
    protected override string GetEntityName() => "Project";
    protected override Expression<Func<Project ,ProjectDto>> ToDto => entity => new ProjectDto
    {
        ProjectId = entity.ProjectId,
        StatusId = entity.StatusId,
        ManagerId = entity.ManagerId,
        PriorityId = entity.PriorityId,
        Title = entity.ProjectName,
        Description = entity.Desc,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        CreationDate = entity.CreationDate,

        StatusName = entity.Status != null ? entity.Status.StatusName : null,
        ManagerName = entity.Manager != null ? entity.Manager.UserFullName : null,
        PriorityName = entity.Priority != null ? entity.Priority.PriorityName : null
    };
    protected override Project ToEntity(CreateProject addDto) => new()
    {
        ProjectName = addDto.Title,
        StatusId = addDto.StatusId,
        ManagerId = addDto.ManagerId,
        PriorityId = addDto.PriorityId,
        Desc = addDto.Description,
        StartDate = addDto.StartDate,
        DueDate = addDto.DueDate,
        EndDate = addDto.EndDate
    };
    protected override void UpdateEntity(Project entity, ProjectDto dto) 
        => entity.ProjectName = dto.Title;
    protected override IQueryable<Project> ApplyDuplicateCheck(IQueryable<Project> query, CreateProject addDto)
        => query.Where(p => p.ProjectName == addDto.Title);
    protected override Guid RelatedTableID => Guid.Parse("73FA5D3A-291D-4043-9120-1FFF6F8C1843");
    protected override Guid GetEntityID(Project entity) => entity.ProjectId;


    public async Task<ProjectDto> EditP(ProjectEditDto projectEditDto)
    {
        var pr = await _projectManagementSystemContext.Projects
            .Include(p => p.Status)
            .Include(p => p.Priority)
            .Include(p => p.Manager)
            .FirstOrDefaultAsync(r => r.ProjectId == projectEditDto.ProjectDtoId)
            ??
            throw new KeyNotFoundException("Project Not Found");

        
        var u = await _projectManagementSystemContext.Users
            .Include(p => p.Role)
            .FirstOrDefaultAsync(r => r.UserId == projectEditDto.ManagerId)
            ??
            throw new KeyNotFoundException("Manager Not Found");


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

        await _eventService.Log
        (
            eventTypeId: Guid.Parse("4C13B6AD-66E9-4532-A0C3-76E7C2BD29E9"),
            tableId: Guid.Parse("682ACE13-5B56-4ED7-BEBA-BE3BC7652BB4"),
            relatedId: pr.ProjectId,
            description: "Edit in Project"
        );

        await EventRegistration("Edit in Project",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }

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
        ??
        throw new KeyNotFoundException("Project Not Found");

        var st = await _projectManagementSystemContext.Statuses.FindAsync(projectEditStatus.StatusId)
        ??
        throw new KeyNotFoundException("Status Not Found");

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
    public async Task<ProjectDto> EditDesc(EditDesc editDesc)
    {
        var pr = await _projectManagementSystemContext.Projects
            .Include(i => i.Status)
            .Include(i => i.Manager)
            .Include(i => i.Priority)
            .FirstOrDefaultAsync(p => p.ProjectId == editDesc.projectId);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");

        pr.Desc = editDesc.Desc;

        await _projectManagementSystemContext.SaveChangesAsync();

        await EventRegistration("Edit in Project, Description",relatedIdG:pr.ProjectId);

        return ProjectToDto(pr);
    }
    
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
    public async Task<ProjectDto> EditStartDate(EditDate editDate)
    {
        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID)
        ??
        throw new KeyNotFoundException("Project Not Found");

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
    public async Task<ProjectDto> EditDueDate(EditDate editDate)
    {
        var pr = await _projectManagementSystemContext.Projects
            .FindAsync(editDate.ID);

        if (pr == null)
            throw new KeyNotFoundException("Project Not Found");

        if (editDate.Date == null)
            throw new InvalidOperationException("Date is Null");

        if (editDate.Date > pr.EndDate || editDate.Date < pr.CreationDate)
            throw new InvalidOperationException("Start Date > End Date or Start Date < Creation Date");

        pr.DueDate = editDate.Date.Value;
        await _projectManagementSystemContext.SaveChangesAsync();
        
        await EventRegistration("Edit in Project, Due Date",relatedIdG:pr.ProjectId);
        
        return ProjectToDto(pr);
    }
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
    
    
    private ProjectDto ProjectToDto(Project project)
    {
        return new ProjectDto
        {
            ProjectId = project.ProjectId,
            StatusId = project.StatusId,
            ManagerId = project.ManagerId,
            PriorityId = project.PriorityId,
            Title = project.ProjectName,
            Description = project.Desc,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            CreationDate = project.CreationDate,
            StatusName = project.Status?.StatusName,
            ManagerName = project.Manager?.UserFullName,
            PriorityName = project.Priority?.PriorityName
        };
    }
    private async Task EventRegistration(string Desc, string? relatedIdS = null,Guid? relatedIdG = null)
    {
        var r = relatedIdS ?? relatedIdG.ToString()
            ??
            throw new InvalidOperationException("RelatedID is null");

        await _eventService.Log
        (
            eventTypeId: Guid.Parse("4C13B6AD-66E9-4532-A0C3-76E7C2BD29E9"),
            tableId: Guid.Parse("682ACE13-5B56-4ED7-BEBA-BE3BC7652BB4"),
            relatedId: Guid.Parse(r),
            description: Desc
        );
    }

}