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

// Configure Swagger with an explicit server URL for production
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TagFlow API", Version = "v1" });
    c.AddServer(new OpenApiServer
    {
        Url = "https://tagflowprobackend-production.up.railway.app",
        Description = "Production server"
    });
});

// Configure CORS to allow calls from both your local frontend and production domain
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", 
                "https://tagflowprobackend-production.up.railway.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable Swagger and configure its endpoint explicitly.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TagFlow API V1");
});

// Redirect HTTP to HTTPS
app.UseHttpsRedirection();

// Apply CORS policy
app.UseCors("AllowFrontend");

// Map SignalR hubs, default route, and controllers
app.MapHub<FileStatusHub>("/file-status-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
