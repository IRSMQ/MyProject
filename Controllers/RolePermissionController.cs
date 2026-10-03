using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;

namespace Test26.Controller;


[Route("api/[controller]")]
[ApiController]
public class RolePermissionController : ControllerBase
{
    private readonly RolePermissionService _rolePermissionService;
    public RolePermissionController(RolePermissionService rolePermissionService)
    {
        _rolePermissionService = rolePermissionService;
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<RPDto>>.Success(await _rolePermissionService.GetAll(),$"Get All Succeeded"));

    [HttpGet("role/{id}")]
    public async Task<IActionResult> GetByRoleId(Guid id)
        => Ok(ApiResponse<List<RPDto>>.Success(await _rolePermissionService.GetByRoleId(id),$"Get By Role Id Succeeded"));

    [HttpGet("permission/{id}")]
    public async Task<IActionResult> GetByPermissionId(Guid id)
        => Ok(ApiResponse<List<RPDto>>.Success(await _rolePermissionService.GetByPermissionId(id),$"Get By Permission Id Succeeded"));

    [HttpPost("add")]
    public async Task<IActionResult> AddRP([FromBody] RPDtoAdd rPDtoAdd)
        => Ok(ApiResponse<RPDto>.Success(await _rolePermissionService.Add(rPDtoAdd),$"Add RP Succeeded"));

    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteRP([FromBody] RPDtoAdd rPDtoAdd)
        => Ok(ApiResponse<RPDto>.Success(await _rolePermissionService.Delete(rPDtoAdd),$"Delete RP Succeeded"));
}