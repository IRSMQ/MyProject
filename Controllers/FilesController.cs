using System.Reflection.Metadata;
using Microsoft.AspNetCore.Mvc;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;

namespace Test26.Controller;

[ApiController]
[Route("api/[controller]/[action]")]
public class FilesController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly IWebHostEnvironment _env;

    public FilesController(ProjectManagementSystemContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] FileUpLoadDto dto)
    {
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
            FileName = dto.File.FileName,
            StoredFileName = uniqueFileName,
            FilePath = fullPath,
            ContentType = dto.File.ContentType,
            FileSize = dto.File.Length
        };

        _context.Documents.Add(doc);
        await _context.SaveChangesAsync();

        return Ok(new { doc.DocumentId, doc.FileName });
    }

    [HttpGet]
    public async Task<IActionResult> Download([FromQuery] Guid id)
    {
        var document = await _context.Documents
            .FindAsync(id)
            ?? throw new KeyNotFoundException("File Not Found");

        if (!System.IO.File.Exists(document.FilePath))
            return NotFound("File Not Exist");

        var memory = new MemoryStream();

        using (var stream = new FileStream(document.FilePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }

        memory.Position = 0;

        return File(memory, document.ContentType, document.FileName);
        
    }
}