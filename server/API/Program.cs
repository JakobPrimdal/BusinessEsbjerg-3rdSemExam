using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Api;
using Api.Jobs;
using Api.Seed;
using Infrastructure;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Infrastructure.Seed;
using LinqToDB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using Service;
using Service.Interfaces;
using Service.Security;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration["ConnectionStrings:Db"] ?? "Data Source=/app/data/db.db;Foreign Keys=True";
var options = new DataOptions().UseSQLite(connectionString);

var userDbOptions = new DataOptions<LoginDb>(options);
builder.Services.AddScoped<LoginDb>(_ => new LoginDb(userDbOptions));
builder.Services.AddScoped<ILoginDb>(sp => sp.GetRequiredService<LoginDb>());

var holidayDbOptions = new DataOptions<HolidayDb>(options);
builder.Services.AddScoped<HolidayDb>(_ => new HolidayDb(holidayDbOptions));

builder.Services.AddScoped<HolidayService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<HolidayRequestService>();
builder.Services.AddScoped<HolidayDecisionService>();

builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IHolidayRuleSetService, HolidayRuleSetService>();

// Admin seeder
builder.Services.AddScoped<AdminSeeder>();
builder.Services.AddScoped<DemoDataSeeder>();

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
            },

            OnChallenge = async context =>
            {
                // Prevent the default ASP.NET Core response.
                context.HandleResponse();

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "You must be authenticated to access this resource."
                });
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Create DB + tables
using (var scope = app.Services.CreateScope())
{
    var loginDb = scope.ServiceProvider.GetRequiredService<LoginDb>();
    DbInitializer.LoginDbInitialize(loginDb);
    
    var holidayDb = scope.ServiceProvider.GetRequiredService<HolidayDb>();
    DbInitializer.HolidayRuleSetDbInitialize(holidayDb, scope.ServiceProvider.GetRequiredService<TimeProvider>());
    
    // Seed initial admin user
    var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    await adminSeeder.SeedAsync();
    
    
    holidayDb.CreateTable<WorkHour>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<HolidayAccount>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<HolidayTransaction>(tableOptions: TableOptions.CreateIfNotExists);
    holidayDb.CreateTable<HolidayRequest>(tableOptions: TableOptions.CreateIfNotExists);
    
    // Development only: demo employee with holiday data (login: employee / password).
    // Never runs in production, so these credentials are not in the cloud database.
    if (app.Environment.IsDevelopment())
    {
        var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await demoSeeder.SeedAsync();
    }
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
