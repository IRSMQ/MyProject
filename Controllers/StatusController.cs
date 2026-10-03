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
        {
            var statuses = await _statusService.GetAll();
            return Ok(ApiResponse<List<StatusDto>>.Success(statuses, "Statuses retrieved successfully."));
        }

        [HttpGet("getByID/{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var status = await _statusService.GetById(id);
            return Ok(ApiResponse<StatusDto>.Success(status, "Status retrieved successfully."));
        }

        [HttpPut("edit")]
        public async Task<IActionResult> Edit([FromBody] StatusDto statusDto)
        {
            var status = await _statusService.Edit(statusDto);
            return Ok(ApiResponse<StatusDto>.Success(status, "Status updated successfully."));
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] StatusAddDto statusAddDto)
        {
            var status = await _statusService.Add(statusAddDto);
            return Ok(ApiResponse<StatusDto>.Success(status, "Status created successfully."));
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var status = await _statusService.Delete(id);
            return Ok(ApiResponse<StatusDto>.Success(status, "Status deleted successfully."));
        }
    }
}
