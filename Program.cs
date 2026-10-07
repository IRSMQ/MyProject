using Test26.Service;
using Test26.Models;
using Test26.Context;
using Microsoft.EntityFrameworkCore;
using AuthLab.Middleware;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens.Experimental;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Test26.ApiR;
using System.Text.Json;
using Test26.PasswordHassher;
using Scalar.AspNetCore;
using Test26.EventS;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<Test26.Context.ProjectManagementSystemContext>(options=>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectManagmentSystem")));

var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Error in JWT KEY"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            if (!context.Handled)
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var response = ApiResponse<object>.Failure(
                    "Authentication failed",
                    new List<string> {"Token not provided"}
                );
                return context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOnly", policy => 
        policy.RequireRole("Manager"));
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<PasswordHassherHandler>();
builder.Services.AddScoped<TaskManagementService>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<RolePermissionService>();
builder.Services.AddScoped<LogService>();

var app = builder.Build();
////////////////////////////////////

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

////////////////////////////////////
app.Run();