using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Evaluation;
using Test26.ApiR;
using Test26.Context;
using Test26.DTOs;
using Test26.Service;
using Test26.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.CodeAnalysis.Differencing;

namespace Test26.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProjectController : ControllerBase
{
    private readonly ProjectService _projectService;
    public ProjectController(ProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<ProjectDto>>.Success(await _projectService.GetAll(),$"Get All Success"));

    [HttpGet("get/{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.GetById(id),$"Get Project By ID Success"));

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProject createProject)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.Add(createProject),$"Project Created"));
    
    [HttpPatch("edit")]
    public async Task<IActionResult> Edit([FromBody] ProjectEditDto projectEditDto)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditP(projectEditDto),$"Edit Succeeded"));

    [HttpPatch("date")]
    public async Task<IActionResult> Date([FromBody] EditAllDate editAllDate)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditDate(editAllDate),$"Edit Date Succeeded"));

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.Delete(id),$"Delete Project Succeeded"));
}