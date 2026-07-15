using VuzScrapper.Scrappers.Common;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed class SpbguRequester(HttpClientWrapper client)
{
    private const string DataLink = "https://enrollelists.spbu.ru/api/reports/priem-list-02/data";

    public async Task<Result<Stream, HttpResponseMessage>> FetchSnapshotStream(string? link)
    {
        var request = await client.GetAsync(link);
        if (!request.IsSuccessStatusCode)
        {
            return Result<Stream, HttpResponseMessage>.Failure(request);
        }

        return Result<Stream, HttpResponseMessage>.Success(await request.Content.ReadAsStreamAsync());
    }

    public async Task<Result<Stream, HttpResponseMessage>> FetchApplicantDataStream(object obj)
    {
        var apiResult = await client.PostAsJsonAsync(DataLink, obj);
        if (!apiResult.IsSuccessStatusCode)
        {
            return Result<Stream, HttpResponseMessage>.Failure(apiResult);
        }

        return Result<Stream, HttpResponseMessage>.Success(await apiResult.Content.ReadAsStreamAsync());
    }
}