using System.Net.Http.Json;

internal sealed class HttpClientWrapper : IDisposable
{
    private readonly HttpClient _client;

    public HttpClientWrapper()
    {
        _client = new HttpClient();
    }

    public async Task<HttpResponseMessage> GetAsync(string? uri)
    {
        return await _client.GetAsync(uri);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync(string? uri, object obj)
    {
        return await _client.PostAsJsonAsync(uri, obj);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}