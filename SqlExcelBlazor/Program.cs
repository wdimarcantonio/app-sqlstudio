using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SqlExcelBlazor;
using SqlExcelBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// FIX: Configura HttpClient con BaseAddress corretto
// Quando l'app è hostata dal server (Hosted Blazor WebAssembly), 
// builder.HostEnvironment.BaseAddress punta già all'origine corretta del server
builder.Services.AddScoped(sp => 
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    return httpClient;
});

// Registra AppState come Scoped (una istanza per sessione utente)
// FIX: Cambiato da Singleton a Scoped per isolare le sessioni degli utenti
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<SqlServerClientService>();
builder.Services.AddScoped<NotificationService>();

// Registra client API SQLite
builder.Services.AddScoped<SqliteApiClient>();

// Registra SQL Load Service
builder.Services.AddScoped<SqlLoadService>();

await builder.Build().RunAsync();
