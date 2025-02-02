using Microsoft.EntityFrameworkCore;
using TagFlowApi.Infrastructure;
using TagFlowApi.Repositories;
using TagFlowApi.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Update to use PostgreSQL
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddDbContext<DataContext>(options =>
// options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithMethods("OPTIONS");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.MapHub<FileStatusHub>("/file-status-hub");

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
