using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.Service;
using Test26.DTOs;
using Azure.Core.Extensions;

namespace Test26.Controller;



[Route("api/[controller]")]
[ApiController]
public class TaskManagementController : ControllerBase
{
    private readonly TaskManagementService _taskManagementService;
    public TaskManagementController(TaskManagementService taskManagementService)
    {
        _taskManagementService = taskManagementService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<TaskManagementDto>>.Success(await _taskManagementService.GetAll() ,$"Get All Succeeded"));

    [HttpGet("getbyid/{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.GetById(id),$"Get By ID Succeeded"));

    [HttpPost("create")]
    public async Task<IActionResult> Create(TMADto tMADto)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Create(tMADto),$"Create Succeeded"));

    [HttpPatch("edit/user")]
    public async Task<IActionResult> EditUser(TaskEditObject taskEditUser)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.EditUser(taskEditUser),$"Edit User Succeeded"));
    
    [HttpPatch("edit/status")]
    public async Task<IActionResult> EditStatus(TaskEditObject taskEditUser)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Status(taskEditUser),$"Edit Status Succeeded"));

    [HttpPatch("edit/priority")]
    public async Task<IActionResult> EditPriority(TaskEditObject taskEditUser)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Priority(taskEditUser),$"Edit Priority Succeeded"));

    [HttpPatch("edit/Title")]
    public async Task<IActionResult> EditTitle(EditString editString)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Title(editString),$"Edit Title Succeeded"));

    [HttpPatch("edit/Desc")]
    public async Task<IActionResult> EditDesc(EditString editString)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Desc(editString),$"Edit Description Succeeded"));

    [HttpPatch("edit/CreationDate")]
    public async Task<IActionResult> EditCreationDate(EditDate editDate)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.CreateDate(editDate),$"Edit Creation Date Succeeded"));

    [HttpPatch("edit/StartDate")]
    public async Task<IActionResult> EditStartDate(EditDate editDate)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.StartDate(editDate),$"Edit Start Date Succeeded"));

    [HttpPatch("edit/DueDate")]
    public async Task<IActionResult> EditDue(EditDate editDate)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.DueDate(editDate),$"Edit Due Succeeded"));

    [HttpPatch("edit/CompletionDate")]
    public async Task<IActionResult> EditCompletionDate(EditDate editDate)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.CompletionDate(editDate),$"Edit Parent Succeeded"));

    [HttpPatch("edit/Parent")]
    public async Task<IActionResult> EditParent(TMEPDto editDate)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Parent(editDate),$"Edit Parent Succeeded"));

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(ApiResponse<TaskManagementDto>.Success(await _taskManagementService.Delete(id),$"Delete Succeeded"));
}
