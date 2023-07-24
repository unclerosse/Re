using System.Security.Cryptography;
using System.Text;

namespace RegApi.Servises;

public static class PasswordHasher
{
    public static string GenerateHash(string password, string salt, string pepper, int iteration)
    {
        if (iteration <= 0) 
            return password;

        var passwordSaltPepper = $"{password}{salt}{pepper}";
        var byteValue = Encoding.UTF8.GetBytes(passwordSaltPepper);
        var byteHash = SHA256.HashData(byteValue);

        var hash = Convert.ToBase64String(byteHash);

        return GenerateHash(hash, salt, pepper, --iteration);
    }

    public static string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();

        var byteSalt = new byte[16];
        rng.GetBytes(byteSalt);
        var salt = Convert.ToBase64String(byteSalt);

        return salt;
    }
}