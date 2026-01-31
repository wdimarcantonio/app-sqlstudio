using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SqlExcelBlazor;
using SqlExcelBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with session handler
builder.Services.AddScoped<SessionHandler>();

// Use AddHttpClient to properly configure with base address and message handler
builder.Services.AddHttpClient("default", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<SessionHandler>();

// Register a scoped HttpClient that uses the named client
builder.Services.AddScoped(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("default");
});

// Registra AppState come Singleton
builder.Services.AddSingleton<AppState>();
builder.Services.AddScoped<SqlServerClientService>();
builder.Services.AddSingleton<NotificationService>();

// Registra client API SQLite
builder.Services.AddScoped<SqliteApiClient>();

// Registra SQL Load Service
builder.Services.AddScoped<SqlLoadService>();

await builder.Build().RunAsync();
