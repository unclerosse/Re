using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RegApi.Models;
using RegApi.Data;
using RegApi.Servises;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "JWT Authorization header using the Bearer scheme",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    o.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);

    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddDbContext<UsersContext>(o => 
    o.UseNpgsql(config.GetConnectionString("UsersContext") + Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")));

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ClockSkew = TimeSpan.FromMinutes(30),
        ValidIssuer = config["Authentication:Schemes:Bearer:ValidIssuer"],
        ValidAudience = config["Authentication:Schemes:Bearer:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_AUTH_KEY")!)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,    
        ValidateIssuerSigningKey = true,
    };
});

builder.Services.AddAuthorization();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<UsersContext>();
        var created = context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error has been found while creating DB");
    }
}

app.UseDeveloperExceptionPage();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/", (HttpContext ctx) => $"Hey").RequireAuthorization();

app.MapPost("/Auth/SignUp", async (UserDto user, UsersContext db, HttpContext ctx) =>
{   
    var service = new UserService(db);

    if (user == null)
        return Results.BadRequest("Invalid User object has been found");
    if (user.UserName == null || user.UserName.Length == 0)
        return Results.BadRequest("Invalid UserName has been found");
    if (user.UserName.Length < 4 || user.UserName.Length > 32)
        return Results.BadRequest("Invalid UserName length has been found");
    if (db.Users!.Any(e => e.UserName == user.UserName))
        return Results.BadRequest($"Attempt of creating existing User '{user.UserName}' has been found");

    string password = user.Password;
    if (!(password.Length >= 8 && password.Length < 256 && password.Any(char.IsUpper) && password.Any(char.IsLower)))
        return Results.BadRequest("Password does not meet the requirements");
    
    user.UserRoles = new List<string>() { "User" };

    var newUser = await service.Register(user);

    return Results.Created("/Auth/SignUp", newUser.UserName);
});

app.MapPost("/Auth/SignIn", async (UserDto user, UsersContext db, HttpContext ctx) =>
{
    var service = new UserService(db);
    
    try
    {   
        var userResponse = await service.Login(user);

        var claims = new List<Claim> 
        { 
            new Claim("UserName", userResponse.UserName),
            new Claim("Id", userResponse.Id),
            new Claim("UserRoles", userResponse.UserRoles.FirstOrDefault(x => x == "Admin", "User")!)
        };
        var jwt = new JwtSecurityToken(
            issuer: config["Authentication:Schemes:Bearer:ValidIssuer"],
            audience: config["Authentication:Schemes:Bearer:ValidAudience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(30)),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_AUTH_KEY")!)
                ), SecurityAlgorithms.HmacSha256
            )
        );

        return Results.Ok(new JwtSecurityTokenHandler().WriteToken(jwt));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(ex.Message);
    }
}
);

app.Run();