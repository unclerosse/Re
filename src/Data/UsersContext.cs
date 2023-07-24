using Microsoft.EntityFrameworkCore;

namespace RegApi.Data;

public class UsersContext : DbContext
{
    public UsersContext(DbContextOptions<UsersContext> options) : base(options) { }

    public DbSet<Models.User>? Users { get; set; }
}