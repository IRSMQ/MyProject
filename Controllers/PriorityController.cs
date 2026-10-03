using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;

namespace Test26.Controller;

[Route("api/[controller]")]
[ApiController]
public class PriorityController : ControllerBase
{
    private readonly PriorityService _priorityService;
    public PriorityController(PriorityService priorityService)
    {
        _priorityService = priorityService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<List<PriorityDto>>.Success(await _priorityService.GetAll(), "Get All Succeeded"));
    }

    [HttpGet("getByID/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(ApiResponse<PriorityDto>.Success(await _priorityService.GetById(id), "Get By ID Succeeded"));
    }

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] PriorityDto priorityDto)
    {
        return Ok(ApiResponse<PriorityDto>.Success(await _priorityService.Edit(priorityDto), "Edit Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] PriorityAddDto priorityAddDto)
    {
        return Ok(ApiResponse<PriorityDto>.Success(await _priorityService.Add(priorityAddDto), "Add Succeeded"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Ok(ApiResponse<PriorityDto>.Success(await _priorityService.Delete(id), "Delete Succeeded"));
    }
}