using Test26.Models;
using Test26.Data;
using Test26.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using NuGet.Common;
using Humanizer;
using Test26.PasswordHassher;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using Test26.EventS;

namespace Test26.Service;

public interface IUserService
{
    public Task<UserDto> EditStatus(UserEditStatus userEditStatus);
    public Task<UserDto> SignUp(UserSignupDto suser);
}


public class UserService : PublicService<User,UserDto,UserSignupDto> ,IUserService
{
    /*
    private readonly ProjectManagementSystemContext _projectManagementSystemContext;
    private readonly TokenService _tokenService;
    private readonly PasswordHassherHandler _passwordHassherHandler;
    public UserService(ProjectManagementSystemContext projectManagementSystemContext, TokenService tokenService, PasswordHassherHandler passwordHassherHandler)
    {
        _projectManagementSystemContext = projectManagementSystemContext;
        _tokenService = tokenService;
        _passwordHassherHandler = passwordHassherHandler;
    }

    
    public async Task<List<UserDto>> GetAll()
    {
        var users = await _projectManagementSystemContext.Users
            .Select(u=> new UserDto
            {
                UserId = u.UserId,
                UserFullName = u.UserFullName,
                Role = u.Role,
                Username = u.Username,
                Phone = u.UserPhone,
                Email = u.UserEmail,
                Status = u.UserStatus
            })
            .ToListAsync();
        if (users.Count == 0)
            throw new InvalidOperationException("There are no users");

        return users;
    }

    public async Task<UserDto> GetById(Guid id)
    {
        var user = await _FindAsync(id);
         
        var newUser = userToDto(user);

        return newUser;
    }

    public async Task<UserDto> Edit(UserDto userDto)
    {
        var user = await _FindAsync(userDto.UserId);

        user.UserId = userDto.UserId;
        user.Role = userDto.Role;
        user.UserFullName = userDto.UserFullName;
        user.Username = userDto.Username;
        user.UserStatus = userDto.Status;
        user.UserEmail = userDto.Email;
        user.UserPhone = userDto.Phone;
        
        await _projectManagementSystemContext.SaveChangesAsync();

        return userDto;
    }

    public async Task<UserDto> Delete(Guid id)
    {
        var user = await _FindAsync(id);

        await _projectManagementSystemContext.UserProjects
            .Where(up=>up.UserId == id)
            .ExecuteDeleteAsync();

        _projectManagementSystemContext.Users.Remove(user);
        await _projectManagementSystemContext.SaveChangesAsync();

        return userToDto(user);
    }

    public async Task<UserDto> EditStatus(Guid id, bool status)
    {
        var user = await _FindAsync(id);

        user.UserStatus = status;

        await _projectManagementSystemContext.SaveChangesAsync();

        return userToDto(user);
    }

    public async Task<string> Login(UserLoginDto userLoginDto)
    {
        var user = await _projectManagementSystemContext.Users
            .FirstOrDefaultAsync(u=>u.Username == userLoginDto.Username);

        if (user == null || !_passwordHassherHandler.Verify(userLoginDto.Password,user.UserPassword))
            throw new KeyNotFoundException($"Incorrect username or password");

        var token = _tokenService.GenerateToken(user);
        return token;
    }

    public async Task<UserDto> SignUp(UserSignupDto suser)
    {
        var user = await _projectManagementSystemContext.Users.Where( u=> u.Username == suser.Username).FirstOrDefaultAsync();

        if (user != null)
            throw new InvalidOperationException("Username Exist");

        if (suser.Username == "" || suser.UserPassword == "")
                throw new ArgumentException("Username or Password is Null");

        var newUser = new User
        {
            UserEmail = suser.UserEmail,
            Username = suser.Username,
            UserFullName = suser.UserFullName,
            UserPassword = _passwordHassherHandler.Hash(suser.UserPassword),
            UserPhone = suser.UserPhone
        };

        _projectManagementSystemContext.Users.Add(newUser);
        await _projectManagementSystemContext.SaveChangesAsync();

        return userToDto(newUser);
    }

    private async Task<User> _FindAsync(Guid id)
    {
        var user = await _projectManagementSystemContext.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User With ID {id} Not Exist");

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

    */
    private readonly PasswordHassherHandler _passwordHassherHandler;
    private readonly TokenService _tokenService;
    public UserService(
        ProjectManagementSystemContext context,
        PasswordHassherHandler passwordHassherHandler,
        TokenService tokenService,
        EventService eventService
        )
    : base(context,eventService)
    {
        _passwordHassherHandler = passwordHassherHandler;
        _tokenService = tokenService;
    }

    protected override Guid GetId(User entity) => entity.UserId;
    protected override Guid GetDtoId(UserDto dto) => dto.UserId;
    protected override string GetEntityName() => "User";
    protected override Expression<Func<User, UserDto>> ToDto => entity => new UserDto
    {
        UserId = entity.UserId,
        RoleId = entity.RoleId.Value,
        UserFullName = entity.UserFullName,
        Username = entity.Username,
        Status = entity.UserStatus,
        Email = entity.UserEmail,
        Phone = entity.UserPhone,
        RoleName = entity.Role.RoleName
    };
    protected override User ToEntity(UserSignupDto addDto) => new()
    {
        UserFullName = addDto.UserFullName,
        Username = addDto.Username,
        UserEmail = addDto.UserEmail,
        UserPhone = addDto.UserPhone,
        UserPassword = addDto.UserPassword,
        UserStatus = addDto.UserStatus
    };
    protected override void UpdateEntity(User user, UserDto userDto)
    {
        user.RoleId = userDto.RoleId;
        user.UserFullName = userDto.UserFullName;
        user.Username = userDto.Username;
        user.UserStatus = userDto.Status;
        user.UserEmail = userDto.Email;
        user.UserPhone = userDto.Phone;
    }
    protected override IQueryable<User> ApplyDuplicateCheck(IQueryable<User> query, UserSignupDto addDto)
        => query.Where(r => r.Username == addDto.Username);
    
    public override async Task<UserDto> GetById(Guid id)
    {
        var entity = await _projectManagementSystemContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(d => d.UserId == id);

        if (entity == null)
            throw new KeyNotFoundException($"{GetEntityName()} With ID: {id} Not Exist");

        return ToDto.Compile()(entity);
    }
    
    public async Task<UserDto> EditStatus(UserEditStatus userEditStatus)
    {
        var user = await _projectManagementSystemContext.Users
            .FindAsync(userEditStatus.Id);

        if (user == null)
            throw new KeyNotFoundException($"User not Found");

        user.UserStatus = userEditStatus.Status;

        await _projectManagementSystemContext.SaveChangesAsync();

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
        var user = await _projectManagementSystemContext.Users
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

        _projectManagementSystemContext.Users.Add(newUser);
        await _projectManagementSystemContext.SaveChangesAsync();

        var role = await _projectManagementSystemContext.Roles
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
        var ur = await _projectManagementSystemContext.Users
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

    protected override Guid RelatedTableID => Guid.Parse("68C0748E-652D-4B94-AA8E-F810E7A447CB");
    protected override Guid GetEntityID(User entity) => entity.UserId;
}