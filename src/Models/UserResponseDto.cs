namespace RegApi.Models;

public class UserResponseDto
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public List<string> UserRoles { get; set; } = null!;
}