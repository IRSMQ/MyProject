using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Test26.PasswordHassher;

public class PasswordHassherHandler
{
    public string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    public bool Verify(string password, string storedHash)
        => Hash(password) == storedHash;
}