var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add HttpContextAccessor (needed for SessionId)
builder.Services.AddHttpContextAccessor();

// Add WorkspaceManager as Singleton (manages all sessions)
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.IWorkspaceManager, SqlExcelBlazor.Server.Services.WorkspaceManager>();

// SqliteService for direct injection (without session isolation)
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.SqliteService>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.ServerExcelService>();

// Add background service for cleanup
builder.Services.AddHostedService<SqlExcelBlazor.Server.Services.SessionCleanupService>();

// Register Data Analysis Services
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.PatternDetector>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.StatisticsCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.QualityScoreCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.ColumnAnalyzer>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.IDataAnalyzerService, SqlExcelBlazor.Server.Services.Analysis.DataAnalyzerService>();

// Configure CORS to allow calls from client (in development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

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
