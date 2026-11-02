using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

using Test26.PasswordHassher;
using Test26.ApiR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
namespace Test26.Controller;


[Route("api/[controller]/[action]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly TokenService _tokenService;
    private readonly LogService _logService;

    public UserController(
        ProjectManagementSystemContext context,
        TokenService tokenService,
        LogService logService
        )
    {
        _context = context;
        _tokenService = tokenService;
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUser()
    {
        var users = await _context.Users
            .Where(e => !e.IsDeleted)
            .Select(e => new UserDto
            {
                UserId = e.UserId,
                RoleId = e.RoleId,
                RoleName = e.Role != null ? e.Role.RoleName : null,
                UserFullName = e.UserFullName,
                Username = e.Username,
                Status = e.UserStatus,
                Email = e.UserEmail,
                Phone = e.UserPhone
            })
            .ToListAsync();

        if (!users.Any())
            throw new ArgumentException("There are no users");

        return Ok(ApiResponse<List<UserDto>>.Success(users, $"Get All Succeeded"));
    }

    [HttpGet]
    public async Task<IActionResult> GetUserById([FromQuery] Guid id)
    {
        var user = await _context.Users
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.UserId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        return Ok(ApiResponse<UserDto>.Success(UserToDto(user),$"Get By ID Succeeded"));
    }

    [HttpPut]
    public async Task<IActionResult> SignUp([FromBody] UserAddDto dto)
    {   

        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        var entity = await _context.Users
            .FirstOrDefaultAsync(e => e.Username == dto.Username && !e.IsDeleted);
        if (entity != null)
            throw new ArgumentException("Username already exists");

        string message = string.Empty;
        string text = string.Empty;

        if (dto.UserId.HasValue)
        {
            await _tokenService.CheckToken(tokenUserId,tokenRoleId, "EditUser");

            entity = await _context.Users
                .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("User Not Found");

            if (dto.Nullable)
            {
                entity.RoleId = dto.RoleId;
                entity.UserEmail = dto.UserEmail;
                entity.UserPhone = dto.UserPhone;
            }

            if (dto.RoleId.HasValue && entity.RoleId != dto.RoleId)
                entity.RoleId = dto.RoleId;

            if (!string.IsNullOrEmpty(dto.UserEmail) && dto.UserEmail != entity.UserEmail)
                entity.UserEmail = dto.UserEmail;

            if (!string.IsNullOrEmpty(dto.UserPhone) && dto.UserPhone != entity.UserPhone)
                entity.UserPhone = dto.UserPhone;

            entity = await _context.Users
                .FirstOrDefaultAsync(e => e.UserId == dto.UserId && !e.IsDeleted)
                ?? throw new KeyNotFoundException("User not Found");

            if (!string.IsNullOrEmpty(dto.UserFullName) && entity.UserFullName != dto.UserFullName)
                entity.UserFullName = dto.UserFullName;

            if (!string.IsNullOrEmpty(dto.Username) && entity.Username != dto.Username)
                entity.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.UserPassword) && entity.UserPassword != PasswordHassherHandler.Hash(dto.UserPassword))
                entity.UserPassword = PasswordHassherHandler.Hash(dto.UserPassword);
            
            message = "Edit Succeeded";
            text = "Edit";
        }
        else
        {
            await _tokenService.CheckToken(tokenUserId, tokenRoleId, "AddUser");
            
            // if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.UserFullName) || string.IsNullOrEmpty(dto.UserPassword))
            //     throw new ArgumentException("Username or User FullName or Password is NULL");

            entity = new User
            {
                UserId = Guid.NewGuid(),
                RoleId = dto.RoleId,
                UserFullName = dto.UserFullName!,
                Username = dto.Username!,
                UserPassword = PasswordHassherHandler.Hash(dto.UserPassword!),
                UserEmail = dto.UserEmail,
                UserPhone = dto.UserPhone
            };

            message = "Add Succeeded";
            text = "Create";
        }

        var entry = _context.Entry(entity);

        await _logService.Log(text, nameof(Models.User), entity.UserId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity), $"{message}"));
    }

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    {
        var entity = await _context.Users
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == userLoginDto.Username && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var isPasswordValid = PasswordHassherHandler.Verify(userLoginDto.Password, entity.UserPassword);

        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid password");

        var token = _tokenService.GenerateToken(entity);

        var newEntity = new UserToken
        {
            UserId = entity.UserId,
            Username = entity.Username,
            Token = token
        };

        return Ok(ApiResponse<UserToken>.Success(newEntity, $"Login Succeeded"));
    }

    [Authorize]
    [HttpPatch]
    public async Task<IActionResult> EditStatus([FromBody] UserEditStatus dto)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "EditStatusUser");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.Id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        if (entity.UserStatus != dto.Status)
        {
            entity.UserStatus = dto.Status;
        }

        var entry = _context.Entry(entity);

        await _logService.Log("Edit", nameof(Models.User), entity.UserId, entry);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Edit Status Succeeded"));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] Guid id)
    {
        var tokenUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Token User Is Null"));
        var tokenRoleName = User.FindFirstValue(ClaimTypes.Role);

        Guid tokenRoleId;

        if (!string.IsNullOrEmpty(tokenRoleName))
            tokenRoleId = Guid.Parse(tokenRoleName);
        else
            throw new UnauthorizedAccessException("Token Role Is Null");

        await _tokenService.CheckToken(tokenUserId,tokenRoleId, "DeleteUser");

        //////////////////////////////////////////////////////
        
        var entity = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");


        /////////////////////////////////////////////////////////
        var entityUserPermission = await _context.UserPermissions
            .Where(e => e.UserId == entity.UserId && !e.IsDeleted)
            .ToListAsync();

        if (entityUserPermission.Count() > 0)
        {
            foreach (var e in entityUserPermission)
            {
                e.IsDeleted = true;
                await _logService.Log("Delete", nameof(UserPermission), e.UserPermissionId);
            }
        }


        /////////////////////////////////////////////////////////
        var entityUserProject = await _context.UserProjects
            .Where(e => e.UserId == entity.UserId && !e.IsDeleted)
            .ToListAsync();

        if (entityUserProject.Count() > 0)
        {
            foreach (var e in entityUserProject)
            {
                e.IsDeleted = true;
                await _logService.Log("Delete", nameof(UserProject), e.UserProjectId);
            }
        }


        /////////////////////////////////////////////////////////
        var entityTaskUser = await _context.TaskUsers
            .Where(e => e.UserId == entity.UserId && !e.IsDeleted)
            .ToListAsync();

        if (entityTaskUser.Count() > 0)
        {
            foreach (var e in entityTaskUser)
            {
                e.IsDeleted = true;
                await _logService.Log("Delete", nameof(TaskUser), e.TaskUserId);
            }
        }

        entity.IsDeleted = true;
        await _logService.Log("Delete", nameof(Models.User), entity.UserId);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Delete Succeeded"));
    }

    private static UserDto UserToDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            RoleId = user.RoleId,
            RoleName = user.Role?.RoleName,
            UserFullName = user.UserFullName,
            Username = user.Username,
            Status = user.UserStatus,
            Email = user.UserEmail,
            Phone = user.UserPhone
        };
    }
}