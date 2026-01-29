var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Aggiungi HttpContextAccessor (necessario per ottenere SessionId)
builder.Services.AddHttpContextAccessor();

// Aggiungi WorkspaceManager come Singleton (gestisce tutte le sessioni)
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.IWorkspaceManager, SqlExcelBlazor.Server.Services.WorkspaceManager>();

// SqliteService rimane Scoped (ma ora usa WorkspaceManager)
builder.Services.AddScoped<SqlExcelBlazor.Server.Services.SqliteService>();
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.ServerExcelService>();

// Aggiungi background service per cleanup
builder.Services.AddHostedService<SqlExcelBlazor.Server.Services.SessionCleanupService>();

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
