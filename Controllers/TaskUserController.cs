using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Microsoft.AspNetCore.Mvc;
using Test26.Service;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]/[action]")]

public class TaskUserController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly IAuthorizationService _authorizationService;

    public TaskUserController(
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
        var entity = await _context.TaskUsers
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Task)
            .Where(e => !e.IsDeleted)
            .Select(e => new TaskUserDto
            {
                TaskId = e.TaskId,
                UserId = e.UserId,
                TaskName = e.Task.Title,
                UserName = e.User.UserFullName
            })
            .ToListAsync();

        if (entity.Count() == 0)
            throw new KeyNotFoundException("Task User Empity");

        return Ok(ApiResponse<List<TaskUserDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] Guid id)
    {
        var entity = await _context.TaskUsers
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Task)
            .Where(e => e.TaskUserId == id && !e.IsDeleted)
            .Select(e => new TaskUserDto
            {
                TaskId = e.TaskId,
                UserId = e.UserId,
                TaskName = e.Task.Title,
                UserName = e.User.UserFullName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Task User Not Found");

        return Ok(ApiResponse<TaskUserDto>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetByTaskId([FromQuery] Guid id)
    {
        var entity = await _context.TaskUsers
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Task)
            .Where(e => e.TaskId == id && !e.IsDeleted)
            .Select(e => new TaskUserDto
            {
                TaskId = e.TaskId,
                UserId = e.UserId,
                TaskName = e.Task.Title,
                UserName = e.User.UserFullName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("Task User Not Found");

        return Ok(ApiResponse<List<TaskUserDto>>.Success(entity,$"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetByUserId([FromQuery] Guid id)
    {
        var entity = await _context.TaskUsers
            .AsNoTracking()
            .Include(e => e.User)
            .Include(e => e.Task)
            .Where(e => e.UserId == id && !e.IsDeleted)
            .Select(e => new TaskUserDto
            {
                TaskId = e.TaskId,
                UserId = e.UserId,
                TaskName = e.Task.Title,
                UserName = e.User.UserFullName
            })
            .ToListAsync()
            ?? throw new KeyNotFoundException("Task User Not Found");

        return Ok(ApiResponse<List<TaskUserDto>>.Success(entity,$"Get All Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] TaskUserDtoAdd dto)
    {
        // var entityTaskManagements = await _context.TaskManagements
        //     .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskId && !e.IsDeleted)
        //     ?? throw new KeyNotFoundException("Task Not Found");

        // var entityUser = await _context.Users
        //     .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
        //     ?? throw new KeyNotFoundException("User Not Found");

        // var entityUserProject = await _context.UserProjects
        //     .Where(e => e.UserId == dto.UserId)
        //     .ToListAsync();


        var tud = new TaskUserDto();
        string message = string.Empty;
        string text = string.Empty;
        var entity = new TaskUser();

        if (dto.TaskUserId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskUser.Edit")).Succeeded)
                return Forbid();

            entity = await _context.TaskUsers
                .FirstOrDefaultAsync(e => e.TaskUserId == dto.TaskUserId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Task User ID Not Found");

            var entityExist = await _context.TaskUsers
                .AnyAsync(e => e.TaskId == dto.TaskId && e.UserId == dto.UserId && !e.IsDeleted);

            if (entityExist)
                throw new ArgumentException("Task User Exist");

            // bool check = true;

            // foreach (var e in entityUserProject)
            // {
            //     if (e.ProjectId == entityTaskManagements.ProjectId)
            //         check = false;
            // }

            // if (check)
            //     throw new ArgumentException("This User Is Not In This Project");

            if (entity.TaskId != dto.TaskId && dto.TaskId.HasValue)
            {
                entity.TaskId = dto.TaskId.Value;
            }

            if (entity.UserId != dto.UserId && dto.UserId.HasValue)
            {
                entity.UserId = dto.UserId.Value;
            }

            message = "Edit Succeeded";
            text = "Edit";
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskUser.Create")).Succeeded)
                return Forbid();

            // bool check = true;

            // foreach (var e in entityUserProject)
            // {
            //     if (e.ProjectId == entityTaskManagements.ProjectId)
            //         check = false;
            // }

            // if (check)
            //     throw new ArgumentException("This User Is Not In This Project");
            if (dto.TaskId == null || dto.UserId == null)
                throw new ArgumentException("Task ID or User ID is Null");
                
            entity = new TaskUser
            {
                TaskUserId = Guid.NewGuid(),
                TaskId = dto.TaskId.Value,
                UserId = dto.UserId.Value
            };

            await _context.TaskUsers.AddAsync(entity);

            message = "Add Succeeded";
            text = "Create";
        }

        var newEntity = await _context.TaskUsers
            .Include(e => e.Task)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.TaskUserId == entity.TaskUserId && !e.IsDeleted);

        tud.TaskUserId = entity.TaskUserId;
        tud.TaskId = entity.TaskId;
        tud.UserId = entity.UserId;
        tud.TaskName = newEntity?.Task.Title;
        tud.UserName = newEntity?.User.UserFullName;

        var entry = _context.Entry(entity);

        await _logService.Log(text, nameof(TaskUser), entity.TaskUserId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskUserDto>.Success(tud, $"{message}"));
    }

    [Authorize(Policy = "TaskUser.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeleteID dto)
    {
        var entity = await _context.TaskUsers
            .Include(e => e.Task)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.TaskUserId == dto.ID && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task User Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(TaskUser), entity.TaskUserId);

        var newTU = new TaskUserDto
        {
            TaskUserId = entity.TaskUserId,
            TaskId = entity.TaskId,
            UserId = entity.UserId,
            TaskName = entity.Task.Title,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskUserDto>.Success(newTU,$"Delete Succeeded"));
    }
}