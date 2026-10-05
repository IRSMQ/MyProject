using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;

namespace Test26.Controller;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly RoleService _roleService;
    public RoleController(RoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<RoleDto>>.Success(await _roleService.GetAll(),$"Get All Succeeded"));

    [HttpGet("getByID/{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<RoleDto>.Success(await _roleService.GetById(id),$"Get By ID"));

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] RoleAddDto roleAddDto)
        => Ok(ApiResponse<RoleDto>.Success(await _roleService.Add(roleAddDto),$"Add Succeeded"));

    [HttpPut("edit")]
    public async Task<IActionResult> Edit([FromBody] RoleDto roleDto)
        => Ok(ApiResponse<RoleDto>.Success(await _roleService.Edit(roleDto),$"Edit Succeeded"));

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(ApiResponse<RoleDto>.Success(await _roleService.Delete(id),$"Delete Succeeded"));
}