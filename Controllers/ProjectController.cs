using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Evaluation;
using Test26.ApiR;
using Test26.Data;
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
    [HttpPatch("edit")]
    public async Task<IActionResult> Edit([FromBody] ProjectEditDto projectEditDto)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditP(projectEditDto),$"Edit Succeeded"));
    
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProject createProject)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.Add(createProject),$"Project Created"));

    [HttpPatch("manager")]
    public async Task<IActionResult> EditManagerProject([FromBody] EditManager editManager)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditManager(editManager),$"Edit Manager Project Success"));

    [HttpPatch("status")]
    public async Task<IActionResult> EditStatus([FromBody] ProjectEditStatus projectEditStatus)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditStatus(projectEditStatus),$"Edit Status Project Success"));

    [HttpPatch("priority")]
    public async Task<IActionResult> EditPriority([FromBody] ProjectEditStatus projectEditStatus)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditPriority(projectEditStatus),$"Edit Priority Project Success"));

    [HttpPatch("desc")]
    public async Task<IActionResult> EditDesc([FromBody] EditDesc editDesc)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditDesc(editDesc),$"Edit Description Project Success"));

    [HttpPatch("startdate")]
    public async Task<IActionResult> EditStartDate([FromBody] EditDate editDate)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditStartDate(editDate),$"Edit Start Date Project Success"));

    [HttpPatch("duedate")]
    public async Task<IActionResult> EditDueDate([FromBody] EditDate editDate)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditDueDate(editDate),$"Edit Due Date Project Success"));

    [HttpPatch("creationdate")]
    public async Task<IActionResult> EditCreationtDate([FromBody] EditDate editDate)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditCreationDate(editDate),$"Edit Creation Date Project Success"));

    [HttpPatch("enddate")]
    public async Task<IActionResult> EditEndDate([FromBody] EditDate editDate)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.EditEndDate(editDate),$"Edit End Date Project Success"));

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(ApiResponse<ProjectDto>.Success(await _projectService.Delete(id),$"Delete Project Succeeded"));
}