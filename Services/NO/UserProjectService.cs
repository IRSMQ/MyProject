/*
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Test26.Data;
using Test26.DTOs;
using Test26.Models;

namespace Test26.Service;

public interface IUserProjectService
{
    public Task<List<UserProjectDto>> GetAll();
    public Task<List<UserProjectDto>> GetByUserId(Guid id);
    public Task<List<UserProjectDto>> GetByProjectId(Guid id);
    public Task<UserProjectDto> AddUserToProject(Guid UserId,Guid projectId);
    //public Task<UserProjectDto> DeleteUserFromProject(Guid UserId,Guid ProjectId);

}


public class UserProjectService : IUserProjectService
{
    private readonly ProjectManagementSystemContext _projectManagementSystemContext;
    public UserProjectService(ProjectManagementSystemContext projectManagementSystemContext)
    {
        _projectManagementSystemContext = projectManagementSystemContext;
    }

    public async Task<List<UserProjectDto>> GetAll()
    {
        var userProjects = await _projectManagementSystemContext.UserProjects
            .Select(up=> new UserProjectDto
            {
                ProjectId = up.ProjectId,
                ProjectName = up.Project.ProjectName,
                UserId = up.UserId,
                UserFullName = up.User.UserFullName
            })
            .ToListAsync();

        if (userProjects.Count == 0)
            throw new InvalidOperationException("There are no UserProject");

        return userProjects;
    }

    public async Task<List<UserProjectDto>> GetByUserId(Guid id)
    {
        var userProject = await _projectManagementSystemContext.UserProjects
            .Where(up => up.UserId == id)
            .Select(up => new UserProjectDto
            {
                ProjectId = up.ProjectId,
                ProjectName = up.Project.ProjectName,
                UserId = up.UserId,
                UserFullName = up.User.UserFullName
            })
            .ToListAsync();

        if (userProject.Count == 0)
            throw new KeyNotFoundException($"User with ID: {id} Not Exist");

        return userProject;
    }

    public async Task<List<UserProjectDto>> GetByProjectId(Guid id)
    {
        var userProject = await _projectManagementSystemContext.UserProjects
            .Where(up => up.ProjectId == id)
            .Select(up=> new UserProjectDto
            {
                ProjectId = up.ProjectId,
                ProjectName = up.Project.ProjectName,
                UserId = up.UserId,
                UserFullName = up.User.UserFullName
            })
            .ToListAsync();

        if (userProject.Count == 0)
            throw new KeyNotFoundException($"Project with ID: {id} Not Exist");

        return userProject;
    }

    public async Task<UserProjectDto> AddUserToProject(Guid projectId,Guid UserId)
    {
        var project = await _projectManagementSystemContext.Projects.FindAsync(projectId);

        if (project == null)
            throw new KeyNotFoundException("Project not exist");
        
        var user = await _projectManagementSystemContext.Users.FindAsync(UserId);

        if (user == null)
            throw new KeyNotFoundException("User not exist");

        var exists = await _projectManagementSystemContext.UserProjects
            .AnyAsync(up => up.UserId == UserId && up.ProjectId == projectId);

        if (exists)
            throw new InvalidOperationException("User is Alredy in project");

        var newUesr = new UserProject
        {
            ProjectId = projectId,
            UserId = UserId
        };

        _projectManagementSystemContext.UserProjects.Add(newUesr);
        await _projectManagementSystemContext.SaveChangesAsync();

        return new UserProjectDto{ProjectId = projectId, UserId = UserId};
    }

    public async Task<UserProjectDto> DeleteUserProject(Guid projectId,Guid userId)
    {
        var project = await _projectManagementSystemContext.Projects.FindAsync(projectId);

        if (project == null)
            throw new KeyNotFoundException("Project Not Exist");

        var user = await _projectManagementSystemContext.Users.FindAsync(userId);

        if (project == null)
            throw new KeyNotFoundException("User Not Exist");

        var exist = await _projectManagementSystemContext.UserProjects
            .AnyAsync(up => up.UserId == userId && up.ProjectId == projectId);

        if (!exist)
            throw new InvalidOperationException("This User Not In project");

        var newUser = new UserProject
        {
            ProjectId = projectId,
            UserId = userId
        };

        _projectManagementSystemContext.UserProjects.Remove(newUser);
        await _projectManagementSystemContext.SaveChangesAsync();

        return new UserProjectDto{ProjectId = projectId, UserId = userId,ProjectName = project.ProjectName,UserFullName = user.UserFullName};
    }
}
*/