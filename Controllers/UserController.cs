using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

using Test26.PasswordHassher;
using Test26.ApiR;
namespace Test26.Controller;
[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ProjectManagementSystemContext _context;
    private readonly TokenService _tokenService;
    private readonly PasswordHassherHandler _passwordHassherHandler;
    private readonly LogService _logService;

    public UserController(
        ProjectManagementSystemContext context,
        TokenService tokenService,
        PasswordHassherHandler passwordHassherHandler,
        LogService logService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHassherHandler = passwordHassherHandler;
        _logService = logService;
    }

    [HttpGet("getall")]
    public async Task<IActionResult> GetAllUser()
    {
        var users = await _context.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new UserDto
            {
                UserId = u.UserId,
                RoleId = u.RoleId,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                UserFullName = u.UserFullName,
                Username = u.Username,
                Status = u.UserStatus,
                Email = u.UserEmail,
                Phone = u.UserPhone
            })
            .ToListAsync();

        if (!users.Any())
            throw new InvalidOperationException("There are no users");

        return Ok(ApiResponse<List<UserDto>>.Success(users,$"Get All Succeeded"));
    }

    [HttpGet("get/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        return Ok(ApiResponse<UserDto>.Success(UserToDto(user),$"Get By ID Succeeded"));
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Add([FromBody] UserAddDto dto)
    {
        var exists = await _context.Users
            .AnyAsync(u => u.Username == dto.Username && !u.IsDeleted);
        if (exists)
            throw new InvalidOperationException("Username already exists");

        var user = new Users();
        string text;

        if (dto.UserId.HasValue)
        {
            var entity = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.UserId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not Found");

            if (!string.IsNullOrEmpty(dto.UserFullName))
                entity.UserFullName = dto.UserFullName;

            if (!string.IsNullOrEmpty(dto.Username))
                entity.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.UserPassword))
                entity.UserPassword = _passwordHassherHandler.Hash(dto.UserPassword);
            

            entity.RoleId = dto.RoleId;
            entity.UserStatus = dto.UserStatus;
            entity.UserEmail = dto.UserEmail;
            entity.UserPhone = dto.UserPhone;

            var entry = _context.Entry(entity);

            var modifiedProps = entry.Properties
                .Where(e => e.IsModified)
                .Select(e => new
                {
                    ColumnName = e.Metadata.Name,
                    OldVal = e.OriginalValue?.ToString() ?? "null",
                    NewVal = e.CurrentValue?.ToString() ?? "null"
                })
                .ToList();

            if (modifiedProps.Any())
            {
                var logId = await _logService.Log("Edit", nameof(Users), entity.UserId);

                foreach (var prop in modifiedProps)
                {
                    await _logService.ChangLog(
                        logId,
                        prop.OldVal,
                        prop.NewVal,
                        prop.ColumnName
                    );
                }
            }

            user = entity;
            text = "Edit Succeeded";

        }
        else
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.UserFullName) || string.IsNullOrEmpty(dto.UserPassword))
                throw new InvalidOperationException("Username or User FullName or Password is NULL");

            var newUser = new Users
            {
                UserId = Guid.NewGuid(),
                RoleId = dto.RoleId,
                UserFullName = dto.UserFullName,
                Username = dto.Username,
                UserPassword = _passwordHassherHandler.Hash(dto.UserPassword),
                UserEmail = dto.UserEmail,
                UserPhone = dto.UserPhone
            };

            await _context.Users.AddAsync(newUser);

            var logId = await _logService.Log("Add", nameof(Users), newUser.UserId);

            var entry = _context.Entry(newUser);
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.Name != "IsDelete")
                {
                    await _logService.ChangLog(
                        logId,
                        "null",
                        prop.CurrentValue?.ToString() ?? "null",
                        prop.Metadata.Name    
                    );
                }
            }

            user = newUser;
            text = "Delete Succeeded";
        }
        

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(user),$"{text}"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    {
        var entity = await _context.Users
            .Include(e => e.Role)
            .FirstOrDefaultAsync(e => e.Username == userLoginDto.Username && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not found");

        var isPasswordValid = _passwordHassherHandler.Verify(userLoginDto.Password, entity.UserPassword);

        if (!isPasswordValid)
            throw new UnauthorizedAccessException("Invalid password");

        var token = _tokenService.GenerateToken(entity);

        return Ok(new UserToken
        {
            UserId = entity.UserId,
            Username = entity.Username,
            Token = token
        });
    }

    [HttpPatch("edit/status")]
    public async Task<IActionResult> EditStatus([FromBody] UserEditStatus dto)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.Id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        if (entity.UserStatus != dto.Status)
        {
            var logId = await _logService.Log("Edit", nameof(Users), entity.UserId);

            await _logService.ChangLog(
                logId,
                entity.UserStatus.ToString(),
                dto.Status.ToString(),
                nameof(entity.UserStatus)
            );
            
            entity.UserStatus = dto.Status;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Edit Status Succeeded"));
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
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
        await _logService.Log("Delete", nameof(Users), entity.UserId);

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Delete Succeeded"));
    }

    private static UserDto UserToDto(Users user)
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