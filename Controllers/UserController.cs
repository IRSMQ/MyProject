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

        var user = new User();
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
                var logId = await _logService.Log("Edit", nameof(User), entity.UserId);

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
            // var newUser = entity;
            // user.UserId = newUser.UserId;
            // user.RoleId = newUser.RoleId;
            // user.UserFullName = newUser.UserFullName;
            // user.Username = newUser.Username;
            // user.Email = newUser.UserEmail;
            // user.Phone = newUser.UserPhone;
            // user.Status = newUser.UserStatus;
            // user.RoleName = newUser.Role.RoleName;

        }
        else
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.UserFullName) || string.IsNullOrEmpty(dto.UserPassword))
                throw new InvalidOperationException("Username or User FullName or Password is NULL");

            var newUser = new User
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

            var logId = await _logService.Log("Add", nameof(User), newUser.UserId);

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

            // user.UserId = newUser.UserId;
            // user.RoleId = newUser.RoleId;
            // user.UserFullName = newUser.UserFullName;
            // user.Username = newUser.Username;
            // user.Email = newUser.UserEmail;
            // user.Phone = newUser.UserPhone;
            // user.Status = newUser.UserStatus;
            // user.RoleName = newUser.Role.RoleName;
        }
        

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(user),$"{text}"));
    }

    // [HttpPost("login")]
    // public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    // {
    //     var user = await _context.Users
    //         .Include(u => u.Role)
    //         .FirstOrDefaultAsync(u => u.Username == userLoginDto.Username && !u.IsDeleted);

    //     if (user == null)
    //         throw new KeyNotFoundException("User not found");

    //     var isPasswordValid = _passwordHassherHandler.Verify(userLoginDto.Password, user.UserPassword);
    //     if (!isPasswordValid)
    //         throw new UnauthorizedAccessException("Invalid password");

    //     var token = _tokenService.CreateToken(user);

    //     return Ok(new UserToken
    //     {
    //         UserId = user.UserId,
    //         Username = user.Username,
    //         Token = token
    //     });
    // }

    // [HttpPut("edit")]
    // public async Task<IActionResult> EditUser([FromBody] UserDto dto)
    // {
    //     var user = await _context.Users
    //         .FirstOrDefaultAsync(u => u.UserId == dto.UserId && !u.IsDeleted)
    //         ?? throw new KeyNotFoundException("User not Found");

    //     user.RoleId = dto.RoleId;
    //     user.UserFullName = dto.UserFullName;
    //     user.Username = dto.Username;
    //     user.UserStatus = dto.Status;
    //     user.UserEmail = dto.Email;
    //     user.UserPhone = dto.Phone;

    //     var entry = _context.Entry(user);
    //     var modifiedProps = entry.Properties.Where(p => p.IsModified).ToList();

    //     if (modifiedProps.Any())
    //     {
    //         var log = _logService.Log("Edit", nameof(User), user.UserId);
    //         await _context.Logs.AddAsync(log);

    //         foreach (var prop in modifiedProps)
    //         {
    //             var changeLog = _logService.ChangLog(
    //                 log.LogId,
    //                 prop.Metadata.Name,
    //                 prop.OriginalValue?.ToString(),
    //                 prop.CurrentValue?.ToString() ?? string.Empty
    //             );
    //             await _context.ChangeLogs.AddAsync(changeLog);
    //         }
    //     }

    //     await _context.SaveChangesAsync();

    //     return Ok(UserToDto(user));
    // }

    [HttpPatch("edit/status")]
    public async Task<IActionResult> EditStatus([FromBody] UserEditStatus dto)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(e => e.UserId == dto.Id && !e.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        if (entity.UserStatus != dto.Status)
        {
            var logId = await _logService.Log("Edit", nameof(User), entity.UserId);

            await _logService.ChangLog(
                logId,
                entity.UserStatus.ToString(),
                dto.Status.ToString(),
                nameof(entity.UserStatus)
            );
            
            entity.UserStatus = dto.Status;
        }
        
        // if (entry.Property(u => u.UserStatus).IsModified)
        // {
        //     var log = _logService.Log("EditStatus", nameof(User), user.UserId);
        //     await _context.Logs.AddAsync(log);

        //     var changeLog = _logService.ChangLog(
        //         log.LogId,
        //         nameof(user.UserStatus),
        //         entry.Property(u => u.UserStatus).OriginalValue?.ToString(),
        //         user.UserStatus.ToString()
        //     );
        //     await _context.ChangeLogs.AddAsync(changeLog);
        // }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Edit Status Succeeded"));
    }

    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var entity = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == id && !u.IsDeleted)
            ?? throw new KeyNotFoundException("User not Found");

        var deleteList = new List<string>();
        entity.IsDeleted = true;
        await _logService.Log("Delete", nameof(User), entity.UserId);
        deleteList.Add(entity.UserId.ToString());

        var entityUserPermission = await _context.UserPermissions
            .Where(e => e.UserId == entity.UserId)
            .ToListAsync();

        if (entityUserPermission.Count() > 0)
        {
            foreach (var userPermission in entityUserPermission)
            {
                userPermission.IsDeleted = true;
                await _logService.Log("Delete", nameof(UserPermission), userPermission.UserPermissionId);
                deleteList.Add(userPermission.UserPermissionId.ToString());
            }
        }

        var entityUserProject = await _context.UserProjects
            .Where(e => e.UserId == entity.UserId)
            .ToListAsync();

        if (entityUserProject.Count() > 0)
        {
            foreach (var userProjects in entityUserProject)
            {
                userProjects.IsDeleted = true;
                await _logService.Log("Delete", nameof(UserProject), userProjects.UserProjectId);
                deleteList.Add(userProjects.UserProjectId.ToString());
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<UserDto>.Success(UserToDto(entity),$"Delete Succeeded\n{deleteList}"));
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