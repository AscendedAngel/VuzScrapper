namespace VuzScrapper.Scrappers.Itmo;

internal sealed class ItmoRequester(HttpClientWrapper client)
{
    public async Task<IEnumerable<HttpResponseMessage>> MakeRequests(IEnumerable<string> links)
        => await Task.WhenAll(from link in links select client.GetAsync(link));
}