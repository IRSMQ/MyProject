using System.Reflection.Metadata;
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
public class FilesController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly LogService _logService;

    public FilesController(
        ProjectManagementSystemContext context,
        IWebHostEnvironment env,
        LogService logService
        )
    {
        _context = context;
        _env = env;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entity = await _context.Documents
            .ToListAsync();

        return Ok(ApiResponse<List<Models.Document>>.Success(entity, "Get All Succeeded"));
    }

    [Authorize]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUpLoadDto dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        ////////////////////////////////////////////////////// 
        
        if (dto.File == null || dto.File.Length == 0)
            return BadRequest("فایلی ارسال نشده است");

        var uploadsFolder = Path.Combine(_env.ContentRootPath, "Uploads");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var extension = Path.GetExtension(dto.File.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await dto.File.CopyToAsync(stream);
        }

        var doc = new Models.Document
        {
            DocumentId = Guid.NewGuid(),
            FileName = dto.File.FileName,
            StoredFileName = uniqueFileName,
            FilePath = fullPath,
            ContentType = dto.File.ContentType,
            FileSize = dto.File.Length
        };

        await _context.Documents.AddAsync(doc);

        var entry = _context.Entry(doc);

        await _logService.Log("Create", nameof(Models.Document), doc.DocumentId, entry);

        await _context.SaveChangesAsync();

        return Ok(new { doc.DocumentId, doc.FileName });
    }

    [HttpGet]
    public async Task<IActionResult> Download([FromQuery] Guid id)
    {
        var entity = await _context.Documents
            .FindAsync(id)
            ?? throw new KeyNotFoundException("File Not Found");

        if (!System.IO.File.Exists(entity.FilePath))
            return NotFound("File Not Exist");

        var memory = new MemoryStream();

        using (var stream = new FileStream(entity.FilePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }

        memory.Position = 0;

        return File(memory, entity.ContentType, entity.FileName);
    }
}