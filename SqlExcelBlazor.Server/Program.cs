var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Registra SQLite Service come Scoped (una istanza per ogni richiesta HTTP/sessione)
// FIX: Cambiato da Singleton a Scoped per isolare le sessioni degli utenti
builder.Services.AddScoped<SqlExcelBlazor.Server.Services.SqliteService>();
// FIX: Cambiato da Singleton a Scoped perché mantiene stato dei file temporanei per sessione
builder.Services.AddScoped<SqlExcelBlazor.Server.Services.ServerExcelService>();

// Registra Data Analysis Services
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.PatternDetector>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.StatisticsCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.QualityScoreCalculator>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.Analysis.ColumnAnalyzer>();
builder.Services.AddScoped<SqlExcelBlazor.Server.Services.Analysis.IDataAnalyzerService, SqlExcelBlazor.Server.Services.Analysis.DataAnalyzerService>();

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
