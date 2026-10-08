using Test26.Models;
using Test26.Context;
using Test26.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using NuGet.Common;
using Humanizer;
using Test26.PasswordHassher;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using Test26.Service;

namespace Test26.Service;

public interface IUserService
{
    public Task<UserDto> EditStatus(UserEditStatus userEditStatus);
    public Task<UserDto> SignUp(UserSignupDto suser);
}


public class UserService : IUserService
{
    
    private readonly ProjectManagementSystemContext _context;
    private readonly TokenService _tokenService;
    private readonly PasswordHassherHandler _passwordHassherHandler;
    private readonly LogService _logService;
    public UserService(ProjectManagementSystemContext context, TokenService tokenService, PasswordHassherHandler passwordHassherHandler, LogService logService)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHassherHandler = passwordHassherHandler;
        _logService = logService;
    }

    
    public async Task<List<UserDto>> GetAll()
    {
        var entity = await _context.Users
            .Select(u=> new UserDto
            {
                UserId = u.UserId,
                RoleId = u.RoleId,
                UserFullName = u.UserFullName,
                Username = u.Username,
                Email = u.UserEmail,
                Phone = u.UserPhone,
                Status = u.UserStatus,
                RoleName = u.Role.RoleName
            })
            .ToListAsync();
        if (entity.Count == 0)
            throw new InvalidOperationException("There are no users");

        return entity;
    }

    public async Task<UserDto> GetById(Guid id)
    {
        var entity = await _FindAsync(id);
         
        var newUser = userToDto(entity);

        return newUser;
    }

    public async Task<UserDto> Edit(UserDto userDto)
    {
        var entity = await _FindAsync(userDto.UserId);

        entity.RoleId = userDto.RoleId;
        entity.UserFullName = userDto.UserFullName;
        entity.Username = userDto.Username;
        entity.UserStatus = userDto.Status;
        entity.UserEmail = userDto.Email;
        entity.UserPhone = userDto.Phone;

        var entry = _context.Entry(entity);

        var modifiedProps = entry.Properties
            .Where(p => p.IsModified)
            .Select( p => new
            {
                ColumnName = p.Metadata.Name,
                OldVal = p.OriginalValue?.ToString() ?? "null",
                NewVal = p.CurrentValue?.ToString() ?? "null"
            })
            .ToList();

        if (modifiedProps.Any())
        {
            var tableName = _context.Model
                .FindEntityType(typeof(User))?
                .GetTableName() ?? nameof(User);

            var logId = await _logService.Log("Edit", tableName, entity.UserId);

            foreach (var change in modifiedProps)
                await _logService.ChangLog(logId, change.OldVal, change.NewVal, change.ColumnName);
        }

        await _context.SaveChangesAsync();

        return userToDto(entity);
    }

    public async Task<UserDto> Delete(Guid id)
    {
        var user = await _FindAsync(id);

        await _context.UserProjects
            .Where(up=>up.UserId == id)
            .ToListAsync();

        

        
        await _context.SaveChangesAsync();

        return userToDto(user);
    }

    public async Task<UserDto> EditStatus(Guid id, bool status)
    {
        var user = await _FindAsync(id);

        user.UserStatus = status;

        await _context.SaveChangesAsync();

        return userToDto(user);
    }

    public async Task<UserDto> EditStatus(UserEditStatus userEditStatus)
    {
        var user = await _context.Users
            .FindAsync(userEditStatus.Id);

        if (user == null)
            throw new KeyNotFoundException($"User not Found");

        user.UserStatus = userEditStatus.Status;

        await _context.SaveChangesAsync();

        return new UserDto
        {
            UserId = user.UserId,
            RoleId = user.RoleId.Value,
            UserFullName = user.UserFullName,
            Username = user.Username,
            Email = user.UserEmail,
            Phone = user.UserPhone,
            Status = user.UserStatus,
            RoleName = user.Role.RoleName

        };
    }
    public async Task<UserDto> SignUp(UserSignupDto suser)
    {
        var user = await _context.Users
            .AnyAsync( u=> u.Username == suser.Username);

        if (user)
            throw new InvalidOperationException("Username Exist");

        if (suser.Username == "" || suser.UserPassword == "")
                throw new ArgumentException("Username or Password is Null");

        var newUser = new User
        {
            RoleId = suser.RoleId,
            UserFullName = suser.UserFullName,
            Username = suser.Username,
            UserPassword = _passwordHassherHandler.Hash(suser.UserPassword),
            UserEmail = suser.UserEmail,
            UserPhone = suser.UserPhone,
            UserStatus = suser.UserStatus
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        var role = await _context.Roles
            .FindAsync(suser.RoleId);

        return new UserDto
        {
            UserId = newUser.UserId,
            RoleId = newUser.RoleId.Value,
            UserFullName = newUser.UserFullName,
            Username = newUser.Username,
            Email = newUser.UserEmail,
            Phone = newUser.UserPhone,
            Status = newUser.UserStatus,
            RoleName = role?.RoleName
        };
    }
    public async Task<UserToken> Login(UserLoginDto userLoginDto)
    {
        var ur = await _context.Users
            .Include(i => i.Role)
            .FirstOrDefaultAsync(u => u.Username == userLoginDto.Username)
        ??
        throw new KeyNotFoundException("Username or Password invalid");
        
        var check = _passwordHassherHandler.Verify(userLoginDto.Password,ur.UserPassword);

        if (!check)
            throw new KeyNotFoundException("Username or Password invalid");

        var token = _tokenService.GenerateToken(ur);
        return new UserToken
        {
            UserId = ur.UserId,
            Username = ur.Username,
            Token = token
        };
    }


    private async Task<User> _FindAsync(Guid id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(p => p.UserId == id && !((ISoftDeletable)p).IsDeleted)
            ?? throw new KeyNotFoundException($"User With ID {id} Not Exist");

        return user;
    }
    private UserDto userToDto(User user)
    {
        var newUser = new UserDto
            {
                UserId = user.UserId,
                UserFullName = user.UserFullName,
                Username = user.Username,
                Status = user.UserStatus,
                Email = user.UserEmail,
                Phone = user.UserPhone
            };

        return newUser;
    }
}




// public async Task<UserDto> SignUp(UserSignupDto suser)
    // {
    //     var user = await _projectManagementSystemContext.Users.Where( u=> u.Username == suser.Username).FirstOrDefaultAsync();

    //     if (user != null)
    //         throw new InvalidOperationException("Username Exist");

    //     if (suser.Username == "" || suser.UserPassword == "")
    //             throw new ArgumentException("Username or Password is Null");

    //     var newUser = new User
    //     {
    //         UserEmail = suser.UserEmail,
    //         Username = suser.Username,
    //         UserFullName = suser.UserFullName,
    //         UserPassword = _passwordHassherHandler.Hash(suser.UserPassword),
    //         UserPhone = suser.UserPhone
    //     };

    //     _projectManagementSystemContext.Users.Add(newUser);
    //     await _projectManagementSystemContext.SaveChangesAsync();

    //     return userToDto(newUser);
    // }