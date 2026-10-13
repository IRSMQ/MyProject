using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test26.Context;
using Test26.DTOs;
using Test26.Models;
using Test26.Service;

using Test26.PasswordHassher;
using Test26.ApiR;
namespace Test26.Controllers
{
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

            var newUser = new User();
            string text;
            // {
            //     UserId = Guid.NewGuid(),
            //     RoleId = dto.RoleId,
            //     UserFullName = dto.UserFullName,
            //     Username = dto.Username,
            //     UserPassword = _passwordHassherHandler.Hash(dto.UserPassword),
            //     UserEmail = dto.UserEmail,
            //     UserPhone = dto.UserPhone,
            //     UserStatus = true,
            //     IsDeleted = false
            // };

            if (dto.UserId.HasValue)
            {
                var entity = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == dto.UserId && !u.IsDeleted)
                    ?? throw new KeyNotFoundException("User not Found");

                entity.RoleId = dto.RoleId;
                entity.UserFullName = dto.UserFullName;
                entity.Username = dto.Username;
                entity.UserStatus = dto.UserStatus;
                entity.UserEmail = dto.UserEmail;
                entity.UserPhone = dto.UserPhone;

                var entry = _context.Entry(entity);
                var modifiedProps = entry.Properties.Where(p => p.IsModified).ToList();

                if (modifiedProps.Any())
                {
                    var log = _logService.Log("Edit", nameof(User), entity.UserId);
                    await _context.Logs.AddAsync(log);

                    foreach (var prop in modifiedProps)
                    {
                        var changeLog = _logService.ChangLog(
                            log.LogId,
                            prop.Metadata.Name,
                            prop.OriginalValue?.ToString(),
                            prop.CurrentValue?.ToString() ?? string.Empty
                        );
                        await _context.ChangeLogs.AddAsync(changeLog);
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(dto.Username) && string.IsNullOrEmpty(dto.UserFullName) && string.IsNullOrEmpty(dto.UserPassword))
                    throw new InvalidOperationException("Username and User FullName and Password is NULL");

                newUser.UserId = Guid.NewGuid();
                newUser.RoleId = dto.RoleId;
                newUser.UserFullName = dto.UserFullName;
                newUser.Username = dto.Username;
                newUser.UserPassword = _passwordHassherHandler.Hash(dto.UserPassword);
                newUser.UserEmail = dto.UserEmail;
                newUser.UserPhone = dto.UserPhone;
                newUser.UserStatus = true;
                newUser.IsDeleted = false;

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
                            prop.CurrentValue?.ToString() ?? string.Empty,
                            prop.Metadata.Name    
                        );
                        
                    }
                }
            }
            

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<UserDto>.Success(UserToDto(newUser),$"SignUp Succeeded"));
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

        [HttpPatch("status")]
        public async Task<IActionResult> EditStatus([FromBody] UserEditStatus userEditStatus)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userEditStatus.Id && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not Found");

            user.UserStatus = userEditStatus.Status;

            var entry = _context.Entry(user);
            if (entry.Property(u => u.UserStatus).IsModified)
            {
                var log = _logService.Log("EditStatus", nameof(User), user.UserId);
                await _context.Logs.AddAsync(log);

                var changeLog = _logService.ChangLog(
                    log.LogId,
                    nameof(user.UserStatus),
                    entry.Property(u => u.UserStatus).OriginalValue?.ToString(),
                    user.UserStatus.ToString()
                );
                await _context.ChangeLogs.AddAsync(changeLog);
            }

            await _context.SaveChangesAsync();

            return Ok(UserToDto(user));
        }

        [HttpDelete("delete/{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not Found");

            user.IsDeleted = true;

            // ثبت لاگ حذف
            var log = _logService.Log("Delete", nameof(User), user.UserId);
            await _context.Logs.AddAsync(log);

            // قطع انتساب پروژه‌های کاربر
            var userProjects = await _context.UserProjects
                .Where(up => up.UserId == id && !up.IsDeleted)
                .ToListAsync();

            foreach (var up in userProjects)
            {
                up.IsDeleted = true;
                var upLog = _logService.Log("Delete", nameof(UserProject), up.UserProjectId);
                await _context.Logs.AddAsync(upLog);
            }

            await _context.SaveChangesAsync();

            return Ok(UserToDto(user));
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
                UserStatus = user.UserStatus,
                UserEmail = user.UserEmail,
                UserPhone = user.UserPhone
            };
        }
    }
}
