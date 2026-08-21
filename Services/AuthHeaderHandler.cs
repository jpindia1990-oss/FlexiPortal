namespace FlexiPortal.Mobile.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // FIX: You save as "Token" not "AuthToken"
        var token = Preferences.Default.Get("Token", "");
        if (string.IsNullOrEmpty(token))
            token = Preferences.Default.Get("AuthToken", ""); // fallback

        if (!string.IsNullOrEmpty(token) && token != "mobile_token")
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}