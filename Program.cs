using FlightClub.Services;
using FlightClub.Services.TaskExecutors;

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
