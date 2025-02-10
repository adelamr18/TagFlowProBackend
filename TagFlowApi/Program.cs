using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TagFlowApi.Infrastructure;
using TagFlowApi.Repositories;
using TagFlowApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Update to use PostgreSQL
// builder.Services.AddDbContext<DataContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories and services
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
                "https://fluffy-chimera-603c00.netlify.app")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TagFlow API V1");
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.MapHub<FileStatusHub>("/file-status-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();