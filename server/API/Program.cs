using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Api;
using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Service;
using Service.Security;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration["ConnectionStrings:Db"] ?? "Data Source=/app/data/db.db;Foreign Keys=True";
var options = new DataOptions().UseSQLite(connectionString);

var userDbOptions = new DataOptions<LoginDb>(options);
builder.Services.AddScoped<LoginDb>(_ => new LoginDb(userDbOptions));

builder.Services.AddScoped<AuthService>();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

builder.Services.AddControllers();
builder.Services.AddCors();

// Swagger
builder.Services.AddOpenApiDocument(c =>
{
    c.AddSecurity("JWT", new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
});

// error handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            RoleClaimType = "role",
            NameClaimType = JwtRegisteredClaimNames.UniqueName
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Create DB + tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LoginDb>();
    db.CreateTable<User>(tableOptions: TableOptions.CreateIfNotExists);
    db.CreateTable<Role>(tableOptions: TableOptions.CreateIfNotExists);

    foreach (var role in Enum.GetValues<UserRole>())
    {
        var roleId = (int)role;

        var exists = db.Roles.Any(r => r.Id == roleId);

        if (!exists)
        {
            db.Insert(new Role
            {
                Id = roleId,
                Name = role.ToString()
            });
        }

    }
}

// CORS
app.UseCors(config => config
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true));

app.UseExceptionHandler(); // Use our custom GlobalExceptionHandler
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

app.Run();
