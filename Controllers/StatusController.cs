using Microsoft.AspNetCore.Mvc;
using Test26.ApiR;
using Test26.DTOs;
using Test26.Service;

namespace Test26.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly StatusService _statusService;

        public StatusController(StatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
            => Ok(ApiResponse<List<StatusDto>>.Success(await _statusService.GetAll(), "Statuses retrieved successfully."));

        [HttpGet("getByID/{id}")]
        public async Task<IActionResult> GetById(Guid id)
            => Ok(ApiResponse<StatusDto>.Success(await _statusService.GetById(id), "Status retrieved successfully."));

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] StatusAddDto statusAddDto)
            => Ok(ApiResponse<StatusDto>.Success(await _statusService.Add(statusAddDto), "Status created successfully."));

        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] StatusDto statusDto)
            => Ok(ApiResponse<StatusDto>.Success(await _statusService.Edit(statusDto), "Status updated successfully."));

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => Ok(ApiResponse<StatusDto>.Success(await _statusService.Delete(id), "Status deleted successfully."));
    }
}
