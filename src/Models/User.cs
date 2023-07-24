namespace RegApi.Models;

public class User
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public List<string> UserRoles { get; set; } = null!;
}