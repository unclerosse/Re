namespace RegApi.Models;

public class UserDto
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public List<string> UserRoles { get; set; } = null!;
}