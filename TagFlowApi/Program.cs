using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TagFlowApi.Infrastructure;
using TagFlowApi.Repositories;
using TagFlowApi.Hubs;
using TagFlowApi.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 500L * 1024 * 1024;
    opts.ValueLengthLimit = int.MaxValue;
    opts.MultipartHeadersLengthLimit = int.MaxValue;
});


builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Update to use PostgreSQL
// builder.Services.AddDbContext<DataContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
        Url = "http://172.29.2.3:5000",
        Description = "Backend VM"
    });


    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API key required. Use header: X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://172.29.2.2:8080",
                "https://172.29.2.2:8080",
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownProxies = { System.Net.IPAddress.Parse("172.29.2.2") }
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseMiddleware<ApiKeyMiddleware>();

app.MapHub<FileStatusHub>("/file-status-hub");
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
