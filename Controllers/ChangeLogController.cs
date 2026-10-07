using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.Models;
using Test26.Service;

namespace Test26.Controller;


[Route("api/[controller]")]
[ApiController]
public class ChangeLogController : ControllerBase
{
    private readonly ChangeLogService _changeLogService;

    public ChangeLogController(ChangeLogService changeLogService)
    {
        _changeLogService = changeLogService;
    }

    [HttpGet("all/{n}")]
    public async Task<IActionResult> GetNTop(int n)
        => Ok(ApiResponse<List<ChangeLog>>.Success(await _changeLogService.GetNTop(n),$"Get {n} Top Succeeded"));

    [HttpGet("logid/{id}")]
    public async Task<IActionResult> GetByLogId(Guid id)
        => Ok(ApiResponse<List<ChangeLog>>.Success(await _changeLogService.GetByLogId(id),$"Get by LogID Succeeded"));

    [HttpGet("id/{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<ChangeLog>.Success(await _changeLogService.GetById(id),$"Get by ID Succeeded"));

    [HttpDelete("id/{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(ApiResponse<ChangeLog>.Success(await _changeLogService.Delete(id),$"Delete Succeeded"));
}