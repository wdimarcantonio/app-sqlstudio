var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registra SQLite Service come Singleton (una istanza per tutta l'app)
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.SqliteService>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.ServerExcelService>();

// Registra Workspace Manager per session isolation
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.WorkspaceManager>();

// Registra Background Services
builder.Services.AddHostedService<SqlExcelBlazor.Server.Services.SessionCleanupService>();

// Registra Data Analysis Services
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.PatternDetector>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.StatisticsCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.QualityScoreCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.ColumnAnalyzer>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.IDataAnalyzerService, SqlExcelBlazor.Server.Services.Analysis.DataAnalyzerService>();

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
