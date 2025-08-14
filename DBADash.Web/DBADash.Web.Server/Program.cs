using DBADash.Web.Core.Services;
using DBADash.Web.Core.Data;
using DBADash.Web.Server.Hubs;
using DBADash.Web.Server.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("ApplicationName", "DBADash.Web")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add support for both Blazor Server and WebAssembly
builder.Services.AddControllersWithViews();

// Entity Framework
builder.Services.AddDbContext<DBADashContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR for real-time updates
builder.Services.AddSignalR();

// Background services with Quartz
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjection();
    
    // Data collection job
    var dataCollectionJobKey = new JobKey("DataCollectionJob");
    q.AddJob<DataCollectionJob>(opts => opts.WithIdentity(dataCollectionJobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(dataCollectionJobKey)
        .WithIdentity("DataCollectionTrigger")
        .WithCronSchedule("0 */5 * * * ?"));  // Every 5 minutes
        
    // Performance monitoring job
    var performanceJobKey = new JobKey("PerformanceMonitoringJob");
    q.AddJob<PerformanceMonitoringJob>(opts => opts.WithIdentity(performanceJobKey));
    
    q.AddTrigger(opts => opts
        .ForJob(performanceJobKey)
        .WithIdentity("PerformanceMonitoringTrigger")
        .WithCronSchedule("0 */1 * * * ?"));  // Every minute
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Core services
builder.Services.AddScoped<IInstanceService, InstanceService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Background monitoring service
builder.Services.AddHostedService<MonitoringBackgroundService>();

// Authentication (if needed)
// builder.Services.AddAuthentication().AddCookie();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Support both Blazor Server and WebAssembly
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

// Authentication middleware (if needed)
// app.UseAuthentication();
// app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapBlazorHub();  // Blazor Server
app.MapFallbackToFile("index.html");  // Blazor WebAssembly fallback

// SignalR hubs
app.MapHub<MonitoringHub>("/hubs/monitoring");
app.MapHub<AlertHub>("/hubs/alerts");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DBADashContext>();
    context.Database.EnsureCreated();
}

try
{
    Log.Information("Starting DBADash Web Application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
