using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Data;
using SqlExcelBlazor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registra SQLite Service come Singleton (una istanza per tutta l'app)
builder.Services.AddSingleton<SqliteService>();
builder.Services.AddSingleton<ServerExcelService>();

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=workflow.db"));

// Add HttpClient for web service calls
builder.Services.AddHttpClient();

// Register workflow services
builder.Services.AddScoped<IWorkflowEngine, WorkflowEngine>();
builder.Services.AddScoped<IStepExecutor, ExecuteQueryStepExecutor>();
builder.Services.AddScoped<IStepExecutor, DataTransferStepExecutor>();
builder.Services.AddScoped<IStepExecutor, WebServiceStepExecutor>();
builder.Services.AddScoped<ExecuteQueryStepExecutor>();

// Configura CORS per permettere chiamate dal client (in sviluppo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
