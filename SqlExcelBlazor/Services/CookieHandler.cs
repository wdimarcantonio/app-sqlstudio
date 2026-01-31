using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Handler that configures HTTP requests to include credentials (cookies)
/// This is needed for session persistence in Blazor WebAssembly
/// </summary>
public class CookieHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // For Blazor WASM, we need to set the credentials mode
        // This tells the browser to include cookies in the request
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        
        return await base.SendAsync(request, cancellationToken);
    }
}
