using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SqlExcelBlazor;
using SqlExcelBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registra AppState come Singleton
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<SqlServerClientService>();
builder.Services.AddSingleton<NotificationService>();

// Registra client API SQLite
builder.Services.AddScoped<SqliteApiClient>();

// Registra SQL Load Service
builder.Services.AddScoped<SqlLoadService>();

await builder.Build().RunAsync();
