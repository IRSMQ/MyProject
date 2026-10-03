namespace Test26.DTOs;

public class UserToken
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Token { get; set; } = null!;
}