using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SqlExcelBlazor;
using SqlExcelBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registra AppState come Singleton
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<SqlServerClientService>();
builder.Services.AddSingleton<NotificationService>();

// Registra client API SQLite
builder.Services.AddScoped<SqliteApiClient>();

// Registra SQL Load Service
builder.Services.AddScoped<SqlLoadService>();

// Registra servizi per architettura ibrida
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<ServerApiClient>();
builder.Services.AddScoped<HybridQueryRouter>();

await builder.Build().RunAsync();
