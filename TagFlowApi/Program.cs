using Microsoft.EntityFrameworkCore;
using TagFlowApi.Infrastructure;
using TagFlowApi.Repositories;
using TagFlowApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<FileRepository>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddSingleton<JwtService>();

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllForSwagger", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
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

app.UseHttpsRedirection();

app.UseWhen(context => context.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
{
    appBuilder.UseCors("AllowAllForSwagger");
});

app.UseCors("AllowFrontend");

app.MapHub<FileStatusHub>("/file-status-hub");

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.MapGet("/auth/login", () => Results.Redirect("https://fluffy-chimera-603c00.netlify.app/auth/login"));

app.Run();
