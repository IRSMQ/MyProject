using Test26.DTOs;
using Test26.Models;
using Test26.Context;
using Microsoft.AspNetCore.Mvc;
using Test26.Service;
using Microsoft.EntityFrameworkCore;
using Test26.ApiR;

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
            .Select(e => new TaskUserDto
            {
                TaskId = e.TaskId,
                UserId = e.UserId,
                TaskName = e.Task.Title,
                UserName = e.User.UserFullName
            })
            .ToListAsync();

        if (entity.Count() > 0)
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

    [HttpPost("add")]
    public async Task<IActionResult> Add(TaskUserDtoAdd dto)
    {
        var exist = await _context.TaskUsers
            .AnyAsync(e => e.TaskId == dto.TaskId && e.UserId == dto.UserId);

        if (exist)
            throw new InvalidOperationException("Task User Exist");

        var existTaskManagements = await _context.TaskManagements
            .AnyAsync(e => e.TaskManagementId == dto.TaskId);

        if (!existTaskManagements)
            throw new KeyNotFoundException("Task Not Found");

        var existUser = await _context.Users
            .AnyAsync(e => e.UserId == dto.UserId);

        if (!existUser)
            throw new KeyNotFoundException("User Not Found");


        var tud = new TaskUserDto();
        string text;

        if (dto.TaskUserId.HasValue)
        {
            var entity = await _context.TaskUsers
                .FindAsync(dto.TaskUserId)
                ?? throw new KeyNotFoundException("Task User ID Not Found");

            var logId = await _logService.Log("Edit",nameof(TaskUser), dto.TaskUserId.Value);

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
            text = "Add Succeeded";
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
            text = "Edit Succeeded";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<TaskUserDto>.Success(tud,$"{text}"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
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