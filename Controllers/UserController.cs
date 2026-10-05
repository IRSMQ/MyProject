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
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    public UserController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAllUser()
        => Ok(ApiResponse<List<UserDto>>.Success(await _userService.GetAll(),$"Get All User Succeeded"));

    [HttpGet("get/{id}")]
    public async Task<IActionResult> GetUserById(Guid id)
        => Ok(ApiResponse<UserDto>.Success(await _userService.GetById(id),$"Get User By ID Succeeded"));

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] UserSignupDto userSignupDto)
        => Ok(ApiResponse<UserDto>.Success(await _userService.SignUp(userSignupDto),$"User Add Succeeded"));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
        => Ok(ApiResponse<UserToken>.Success(await _userService.Login(userLoginDto),$"Login Succeeded"));

    [HttpPut("edit")]
    public async Task<IActionResult> EditUser([FromBody] UserDto userDto)
        => Ok(ApiResponse<UserDto>.Success(await _userService.Edit(userDto),$"Edit User Succeeded"));

    [HttpPatch("status")]
    public async Task<IActionResult> EditStatus([FromBody] UserEditStatus userEditStatus)
        => Ok(ApiResponse<UserDto>.Success(await _userService.EditStatus(userEditStatus),$"Edit Status Succeeded"));
    
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
        => Ok(ApiResponse<UserDto>.Success(await _userService.Delete(id),$"Delete User With ID {id} Success"));
}