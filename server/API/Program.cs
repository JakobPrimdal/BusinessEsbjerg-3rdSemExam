using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Api;
using Api.Jobs;
using Api.Seed;
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

var holidayDbOptions = new DataOptions<HolidayDb>(options);
builder.Services.AddScoped<HolidayDb>(_ => new HolidayDb(holidayDbOptions));

builder.Services.AddScoped<AuthService>();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

// Admin seeder
builder.Services.AddScoped<AdminSeeder>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<HolidayService>();
builder.Services.AddHostedService<MonthlyHolidayScheduler>();

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

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // If the "jwt" cookie exists, use it as the token.
                // Otherwise, use normal Authorization
                if (ctx.Request.Cookies.TryGetValue("jwt", out var token))
                    ctx.Token = token;
                
                return Task.CompletedTask;
            }
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

    // Seed roles
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
    
    // Seed initial admin user
    var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    adminSeeder.Seed();
    
    var holidayDb = scope.ServiceProvider.GetRequiredService<HolidayDb>();
    holidayDb.CreateTable<RuleValue>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<WorkHour>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<Salary>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<HolidayAccount>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<HolidayTransaction>(tableOptions: TableOptions.CreateIfNotExists);
 
    void SeedRule(string key, string value)
    {
        if (holidayDb.RuleValues.Any(r => r.Key == key)) return;
        holidayDb.Insert(new RuleValue
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            ValidFrom = new DateOnly(2020, 9, 1),
            CreatedAt = DateTime.UtcNow
        });
    }
    SeedRule(HolidayService.DaysPerMonthKey, "2.08");
    SeedRule(HolidayService.Week6HoursPerMonthKey, "3.083");
}

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

app.UseCors(config => config
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins(allowedOrigins));

app.UseExceptionHandler(); // Use our custom GlobalExceptionHandler
app.UseAuthentication();
app.UseAuthorization();

// Swagger
app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

app.Run();
