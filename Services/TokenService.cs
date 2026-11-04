
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Test26.Models;
using Test26.DTOs;
using Test26.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Test26.Service;

public class TokenService
{
    private readonly IConfiguration _configuration;
    private readonly ProjectManagementSystemContext _context;
    public TokenService(IConfiguration configuration, ProjectManagementSystemContext context)
    {
        _context = context;
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Role, user.RoleId.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public async Task CheckToken(Guid tokenUserId, Guid? tokenRoleId, string PermissionTableName){

        if (string.IsNullOrEmpty(tokenRoleId.ToString()))
            throw new UnauthorizedAccessException("Token Role Is Null");

        var entityRole = await _context.Roles
            .FirstOrDefaultAsync(e => e.RoleId == tokenRoleId)
            ?? throw new KeyNotFoundException("Role Not Found");

        if (entityRole.RoleName != "Manager")
        {
            var entityUserPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(e => e.UserId == tokenUserId && e.Permission.PermissionName == PermissionTableName && !e.IsDeleted);

            if (entityUserPermission != null)
            {
                if (!entityUserPermission.Bit)
                {
                    throw new UnauthorizedAccessException("This User Does Not Have Access Permission");
                }
            }
            else
            {
                var entityRolePermission = await _context.RolePermissions
                    .AnyAsync(e => e.RoleId == tokenRoleId && e.Permission.PermissionName == PermissionTableName && !e.IsDeleted);

                if (!entityRolePermission)
                {
                    throw new UnauthorizedAccessException("This User Does Not Have Access Permission");
                }
            }
        }
    }
}