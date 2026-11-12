using Infrastructure;
using Infrastructure.Entities;
using LinqToDB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApiDocument();
// ---------- database ----------
var provider = builder.Configuration["DB_PROVIDER"] ?? "sqlite";
var connectionString = builder.Configuration["DB"]
                       ?? "Data Source=holiday.db;Foreign Keys=True";

var options = provider == "postgres"
    ? new DataOptions().UsePostgreSQL(connectionString)
    : new DataOptions().UseSQLite(connectionString);

builder.Services.AddSingleton(new DataOptions<HolidayDb>(options));
builder.Services.AddScoped<HolidayDb>();

var app = builder.Build();
//-temporarily

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HolidayDb>();
    db.CreateTable<RuleValue>(tableOptions: TableOptions.CreateIfNotExists);
    db.CreateTable<WorkHour>(tableOptions: TableOptions.CreateIfNotExists);
    db.CreateTable<Salary>(tableOptions: TableOptions.CreateIfNotExists);
    db.CreateTable<HolidayAccount>(tableOptions: TableOptions.CreateIfNotExists);
    db.CreateTable<HolidayTransaction>(tableOptions: TableOptions.CreateIfNotExists);
}
app.UseCors(config => config
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetIsOriginAllowed(_ => true));

app.UseOpenApi();
app.UseSwaggerUi();

app.MapControllers();

app.Run();
