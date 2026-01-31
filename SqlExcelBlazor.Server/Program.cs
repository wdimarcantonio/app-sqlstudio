var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add session support for session isolation
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    
    // SameSite.None with Secure=true works with HTTPS (even on localhost)
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    
    // Set explicit cookie name for easier debugging
    options.Cookie.Name = ".SqlStudio.Session";
});

// Add HttpContextAccessor (needed for SessionId)
builder.Services.AddHttpContextAccessor();

// Add WorkspaceManager as Singleton (manages all sessions)
builder.Services.AddSingleton<SqlExcelBlazor.Server.Services.IWorkspaceManager, SqlExcelBlazor.Server.Services.WorkspaceManager>();

// Other services
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
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // In development, allow localhost with credentials
                policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                // In production, configure specific origins
                policy.WithOrigins("https://yourdomain.com")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
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

// CORS must come BEFORE Session so that CORS headers are set before session cookie is sent
app.UseCors("AllowAll");

// Session middleware must come AFTER CORS but BEFORE endpoints
app.UseSession();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
