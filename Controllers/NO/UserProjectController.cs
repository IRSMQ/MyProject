/*
using Test26.Service;
using Test26.ApiR;
using Microsoft.AspNetCore.Mvc;
using Test26.DTOs;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Test26.Models;
using Microsoft.AspNetCore.Authorization;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Test26.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "ManagerOnly")]
public class UserProjectController : ControllerBase
{
    private readonly UserProjectService _userProjectService;
    public UserProjectController(UserProjectService userProjectService)
    {
        _userProjectService = userProjectService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(ApiResponse<List<UserProjectDto>>.Success(await _userProjectService.GetAll(),$"Get All Succeeded"));
    }

    [HttpGet("getbyprojectid/{id}")]
    public async Task<IActionResult> GetByProjectId(Guid id)
    {
        return Ok(ApiResponse<List<UserProjectDto>>.Success(await _userProjectService.GetByProjectId(id),$"Get By Project ID Succeeded"));
    }

    [HttpGet("getbyuserid/{id}")]
    public async Task<IActionResult> GetByUserId(Guid id)
    {
        return Ok(ApiResponse<List<UserProjectDto>>.Success(await _userProjectService.GetByUserId(id),$"Get By User ID Succeeded"));
    }

    [HttpPost("AddUserToProject")]
    public async Task<IActionResult> AddUserToProject([FromBody] AddUPD userProjectDto)
    {
        return Ok(ApiResponse<UserProjectDto>.Success(await _userProjectService.AddUserToProject(userProjectDto.ProjectId,userProjectDto.UserId),$"User Add Succeeded"));
    }

    [HttpDelete("RemoveUserFromProject")]
    public async Task<IActionResult> RemoveUserFromProject([FromBody] AddUPD addUPD)
    {
        return Ok(ApiResponse<UserProjectDto>.Success(await _userProjectService.DeleteUserProject(addUPD.ProjectId,addUPD.UserId),$"Remove Succeeded"));
    }
}
*/