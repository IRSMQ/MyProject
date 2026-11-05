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
public class TaskFileController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly LogService _logService;
    private readonly IAuthorizationService _authorizationService;

    public TaskFileController(
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
        var entity = await _context.TaskDocuments
            .Include(e => e.Document)
            .Include(e => e.Task)
            .Where(e => !e.IsDeleted)
            .Select(e => new TaskDocumentDto
            {
                TaskDocumentId = e.TaskDocumentId,
                TaskId = e.TaskId,
                DocumentId = e.DocumentId,
                TaskTitle = e.Task.Title,
                DocumentName = e.Document.FileName
            })
            .ToListAsync();

        return Ok(ApiResponse<List<TaskDocumentDto>>.Success(entity, "Get All Succeeded"));    
    }

    [HttpGet]
    public async Task<IActionResult> GetByID([FromQuery] Guid id)
    {
        var entity = await _context.TaskDocuments
            .Include(e => e.Document)
            .Include(e => e.Task)
            .Where(e => e.TaskDocumentId == id && !e.IsDeleted)
            .Select(e => new TaskDocumentDto
            {
                TaskDocumentId = e.TaskDocumentId,
                TaskId = e.TaskId,
                DocumentId = e.DocumentId,
                TaskTitle = e.Task.Title,
                DocumentName = e.Document.FileName
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Task Document Not Found");

        return Ok(ApiResponse<TaskDocumentDto>.Success(entity, "Get By ID Succeeded"));
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Put(TaskDocumentAddDto dto)
    {
        string message = string.Empty;
        string textLog = string.Empty;
        TaskDocument entity;

        if (dto.TaskDocumentId.HasValue)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskFile.Edit")).Succeeded)
                return Forbid();

            entity = await _context.TaskDocuments
                .Include(e => e.Task)
                .Include(e => e.Document)
                .FirstOrDefaultAsync(e => e.TaskDocumentId == dto.TaskDocumentId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("Task Document Not Found");

            if (dto.TaskId.HasValue)
                entity.TaskId = dto.TaskId.Value;

            if (dto.DocumentId.HasValue)
                entity.DocumentId = dto.DocumentId.Value;

            message = "Edit Succeeded";
            textLog = "Edit";
        }
        else
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "TaskFile.Create")).Succeeded)
                return Forbid();

            if (!dto.TaskId.HasValue || !dto.DocumentId.HasValue)
                throw new ArgumentException("Task ID or Document ID is Null");

            var exist = await _context.TaskDocuments
                .AnyAsync(e => e.TaskId == dto.TaskId && e.DocumentId == dto.DocumentId && !e.IsDeleted);

            if (exist)
                throw new ArgumentException("Task Document Exist");

            entity = new TaskDocument
            {
                TaskDocumentId = Guid.NewGuid(),
                TaskId = dto.TaskId.Value,
                DocumentId = dto.DocumentId.Value
            };

            await _context.TaskDocuments.AddAsync(entity);

            message = "Add Succeeded";
            textLog = "Create";
        }

        var entry = _context.Entry(entity);

        await _logService.Log(textLog, nameof(TaskDocument), entity.TaskDocumentId, entry);

        await _context.SaveChangesAsync();

        var entityTaskManagement = await _context.TaskManagements.FindAsync(entity.TaskId);
        var entityDocument = await _context.Documents.FindAsync(entity.DocumentId);

        var newEntity = new TaskDocumentDto
        {
            TaskDocumentId = entity.TaskDocumentId,
            TaskId = entity.TaskId,
            DocumentId = entity.DocumentId,
            TaskTitle = entityTaskManagement?.Title,
            DocumentName = entityDocument?.FileName
        };

        return Ok(ApiResponse<TaskDocumentDto>.Success(newEntity, $"{message}"));
    }

    [Authorize(Policy = "TaskFile.Delete")]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] Guid id)
    {
        var entity = await _context.TaskDocuments
            .Include(e => e.Document)
            .Include(e => e.Task)
            .FirstOrDefaultAsync(e => e.DocumentId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("Task Document Not Found");

        entity.IsDeleted = true;

        await _logService.Log("Delete", nameof(TaskDocument), entity.DocumentId);

        var newEntity = new TaskDocumentDto
        {
            DocumentId = entity.DocumentId,
        };

        return Ok(new {entity.TaskDocumentId, entity.TaskId, entity.DocumentId, entity.Task.Title, entity.Document.FileName});
    }
}