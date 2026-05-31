using System.Text.Json.Serialization;
using MESLite.API.Hubs;
using MESLite.Application;
using MESLite.Application.Common.Interfaces;
using MESLite.Infrastructure;
using MESLite.Infrastructure.Persistence;
using MESLite.Simulator;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Serilog -------------------------------------------------------------
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/meslite-.log", rollingInterval: Serilog.RollingInterval.Day));

const string CorsPolicy = "MESLiteCors";

// --- Services ------------------------------------------------------------
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSimulator(builder.Configuration);

// SignalR + the notifier bridge so Application/Simulator can push without knowing about SignalR.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IProductionNotifier, SignalRProductionNotifier>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MES Lite API", Version = "v1", Description = "Textile Production Monitoring & OEE Dashboard" });
});

builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                 ?? new[] { "http://localhost:5173", "http://localhost:3000" })
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

// --- Database migrate + seed --------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DbInitializer.InitializeAsync(db, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed.");
        throw;
    }
}

// --- HTTP pipeline -------------------------------------------------------
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MES Lite API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors(CorsPolicy);

app.MapControllers();
app.MapHub<ProductionHub>("/hubs/production");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }
