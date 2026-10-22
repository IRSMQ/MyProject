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
[Route("api/[controller]")]

public class TaskUserController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;

    public TaskUserController(ProjectManagementSystemContext context, LogService logService)
    {
        _context = context;
        _logService = logService;
    }

    [HttpGet("getall")]
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

    [HttpGet("getbyid")]
    public async Task<IActionResult> GetById(Guid id)
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
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Task User Not Found");

        return Ok(ApiResponse<TaskUserDto>.Success(entity,$"Get All Succeeded"));
    }

    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> Add(TaskUserDtoAdd dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tokenRoleId = Guid.Parse(User.FindFirstValue(ClaimTypes.Role));

        await _logService.CheckToken(tokenUserId,tokenRoleId, "AddTaskUser");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskUsers
            .FirstOrDefaultAsync(e => e.TaskId == dto.TaskId && e.UserId == dto.UserId && !e.IsDeleted);

        if (entity != null)
            throw new InvalidOperationException("Task User Exist");

        var entityTaskManagements = await _context.TaskManagements
            .FirstOrDefaultAsync(e => e.TaskManagementId == dto.TaskId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Not Found");

        var entityUser = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User Not Found");


        var tud = new TaskUserDto();
        string text;

        if (dto.TaskUserId.HasValue)
        {
            entity = await _context.TaskUsers
                .FindAsync(dto.TaskUserId)
                ?? throw new KeyNotFoundException("Task User ID Not Found");

            var logId = await _logService.Log("Edit", nameof(TaskUser), dto.TaskUserId.Value);

            if (entity.TaskId != dto.TaskId && dto.TaskId.HasValue)
            {
                await _logService.ChangLog(
                    logId,
                    entity.TaskId.ToString(),
                    dto.TaskId.ToString(),
                    nameof(entity.TaskId)
                );
                entity.TaskId = dto.TaskId.Value;
            }

            if (entity.UserId != dto.UserId && dto.UserId.HasValue)
            {
                await _logService.ChangLog(
                    logId,
                    entity.UserId.ToString(),
                    dto.UserId.ToString(),
                    nameof(entity.TaskId)
                );
                entity.UserId = dto.UserId.Value;
            }

            tud.TaskId = entity.TaskId;
            tud.UserId = entity.UserId;
            tud.TaskName = entity.Task.Title;
            tud.UserName = entity.User.UserFullName;
            text = "Edit Succeeded";
        }
        else
        {
            var newTU = new TaskUser
            {
                TaskUserId = Guid.NewGuid(),
                TaskId = dto.TaskId.Value,
                UserId = dto.UserId.Value
            };

            await _context.TaskUsers.AddAsync(newTU);
            
            var entry = _context.Entry(newTU);

            var logId = await _logService.Log("Create", nameof(TaskUser), newTU.TaskUserId);

            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.Name != "IsDeleted")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name
                    );
                }
            }

            tud.TaskId = newTU.TaskId;
            tud.UserId = newTU.UserId;
            tud.TaskName = newTU.Task.Title;
            tud.UserName = newTU.User.UserFullName;
            text = "Add Succeeded";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskUserDto>.Success(tud,$"{text}"));
    }

    [Authorize]
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var tokenRoleId = Guid.Parse(User.FindFirstValue(ClaimTypes.Role));

        await _logService.CheckToken(tokenUserId,tokenRoleId, "DeleteTaskUser");

        //////////////////////////////////////////////////////
        
        var entity = await _context.TaskUsers
            .FindAsync(id)
            ?? throw new KeyNotFoundException("Task User Not Found");

        entity.IsDeleted = true;

        await _logService.Log("delete", nameof(TaskUser), entity.TaskUserId);

        var newTU = new TaskUserDto
        {
            TaskId = entity.TaskId,
            UserId = entity.UserId,
            TaskName = entity.Task.Title,
            UserName = entity.User.UserFullName
        };

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskUserDto>.Success(newTU,$"Delete Succeeded"));
    }
}