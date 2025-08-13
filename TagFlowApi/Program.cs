using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TagFlowApi.Infrastructure;
using TagFlowApi.Repositories;
using TagFlowApi.Hubs;
using TagFlowApi.Middlewares;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(o =>
{
    o.FormatterName = ConsoleFormatterNames.Json;
    o.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Services.Configure<JsonConsoleFormatterOptions>(o =>
{
    o.UseUtcTimestamp = true;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Logging.AddFilter("TagFlowApi", LogLevel.Warning);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// builder.Services.AddDbContext<DataContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FileRepository>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddSingleton<JwtService>();

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TagFlow API", Version = "v1" });
    c.AddServer(new OpenApiServer
    {
        Url = "https://tagflowprobackend-production.up.railway.app",
        Description = "Production server"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://tagflowprobackend-production.up.railway.app",
                "https://fluffy-chimera-603c00.netlify.app",
                 "https://fluffy-chimera-603c00.netlify.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseMiddleware<RequestLoggingAndErrorMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TagFlow API V1");
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseMiddleware<ApiKeyMiddleware>();

app.MapHub<FileStatusHub>("/file-status-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
