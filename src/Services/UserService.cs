using Microsoft.EntityFrameworkCore;
using RegApi.Data;
using RegApi.Models;

namespace RegApi.Servises;

public class UserService
{
    private readonly UsersContext _db;
    private readonly string _pepper;
    private readonly int _iteration = 3;

    public UserService(UsersContext db)
    {
        _db = db;
        _pepper = Environment.GetEnvironmentVariable("PAPPER")!;
    }

    public async Task<UserResponseDto> Register(UserDto userDto)
    {
        var user = new User()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userDto.UserName,
            UserRoles = userDto.UserRoles,
            PasswordSalt = PasswordHasher.GenerateSalt()
        };

        user.PasswordHash = PasswordHasher.GenerateHash(userDto.Password, user.PasswordSalt, _pepper, _iteration);
        await _db.Users!.AddAsync(user);
        await _db.SaveChangesAsync();

        return new UserResponseDto() 
        {
            Id = user.Id,
            UserName = user.UserName,
        };
    }

    public async Task<UserResponseDto> Login(UserDto userDto)
    {
        var user = await _db.Users!.FirstOrDefaultAsync(x => 
            x.UserName == userDto.UserName) ?? throw new Exception("Non existing UserName has been found");

        var passwordHash = PasswordHasher.GenerateHash(userDto.Password, user.PasswordSalt, _pepper, _iteration);
        if (user.PasswordHash != passwordHash)
            throw new Exception("Invalid password has been found");
        
        return new UserResponseDto() 
        {
            Id = user.Id,
            UserName = user.UserName,
            UserRoles = user.UserRoles
        };
    }

}