using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;

namespace Test26.Controller;

[Route("api/[controller]")]
[ApiController]
public class PermissionController : ControllerBase
{
    private readonly PermissionService _permissionService;
    public PermissionController(PermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<List<PermissionDto>>.Success(await _permissionService.GetAll(),$"Get All Succeeded"));
    }

    [HttpGet("getByID/{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(ApiResponse<PermissionDto>.Success(await _permissionService.GetById(id),$"Get By ID"));
    }

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] PermissionDto permissionDto)
    {
        return Ok(ApiResponse<PermissionDto>.Success(await _permissionService.Edit(permissionDto),$"Edit Succeeded"));
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] PermissionAddDto permissionAddDto)
    {
        return Ok(ApiResponse<PermissionDto>.Success(await _permissionService.Add(permissionAddDto),$"Add Succeeded"));
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return Ok(ApiResponse<PermissionDto>.Success(await _permissionService.Delete(id),$"Delete Succeeded"));
    }
}