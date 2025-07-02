using FlightClub.Services;
using FlightClub.Services.TaskExecutors;
using FlightClub.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add API controllers with JSON options
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original property names
        options.JsonSerializerOptions.WriteIndented = true; // Pretty print JSON
    });

// Add Swagger/OpenAPI support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FlightClub API", Version = "v1" });
    
    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Entity Framework with SQLite
// Database will be created in wwwroot directory
var wwwrootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? $"Data Source={Path.Combine(wwwrootPath, "database.db")}";

// Replace relative path with absolute path
if (connectionString.Contains("wwwroot/database.db"))
{
    connectionString = $"Data Source={Path.Combine(wwwrootPath, "database.db")}";
}

builder.Services.AddDbContext<FlightClubDbContext>(options =>
    options.UseSqlite(connectionString));

// Register application services
builder.Services.AddScoped<IScheduledTaskService, ScheduledTaskService>();

// Register task executors
// builder.Services.AddScoped<ITaskExecutor, NotificationTaskExecutor>(); // DISABLED - Notification task type disabled
builder.Services.AddHttpClient<ReserveBuntzenExecutor>();
builder.Services.AddScoped<ITaskExecutor, ReserveBuntzenExecutor>();

// Register task execution services
builder.Services.AddScoped<ITaskExecutionService, TaskExecutionService>();
builder.Services.AddScoped<ITaskTriggerService, TaskTriggerService>();

// Register background service for task scheduling
builder.Services.AddHostedService<TaskSchedulerService>();

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FlightClubDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Ensure wwwroot directory exists
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    if (!Directory.Exists(wwwrootPath))
    {
        Directory.CreateDirectory(wwwrootPath);
        logger.LogInformation("Created wwwroot directory at: {Path}", wwwrootPath);
    }
    
    // Initialize database
    await DatabaseInitializer.InitializeAsync(context, logger);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure Swagger for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlightClub API v1");
        c.RoutePrefix = "api-docs"; // Serve Swagger UI at /api-docs
    });
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Map API controllers
app.MapControllers();

// Map MVC controllers
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
