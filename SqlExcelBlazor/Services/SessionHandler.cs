using Microsoft.JSInterop;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Gestisce il Session ID usando sessionStorage invece dei cookie
/// </summary>
public class SessionHandler : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private string? _sessionId;

    public SessionHandler(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Ottieni o crea Session ID
        if (_sessionId == null)
        {
            try
            {
                _sessionId = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", "app-session-id");
                if (string.IsNullOrEmpty(_sessionId))
                {
                    _sessionId = Guid.NewGuid().ToString();
                    await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "app-session-id", _sessionId);
                }
            }
            catch
            {
                // Fallback se sessionStorage non è disponibile
                _sessionId = Guid.NewGuid().ToString();
            }
        }
        
        // Aggiungi header personalizzato con Session ID
        request.Headers.Add("X-Session-Id", _sessionId);
        
        return await base.SendAsync(request, cancellationToken);
    }
}
