namespace VuzScrapper.Scrappers.Itmo;

internal sealed class ItmoScrapper(HttpClientWrapper client) : IRequestParser
{
    public string Name => "ФГАОУ ВО \"НИУ ИТМО\"";

    private readonly ItmoRequester _itmoRequester = new(client);
    private readonly ItmoParser _itmoParser = new();

    public async Task<Result<Competition, List<HttpResponseMessage>>> CreateCompetition()
    {
        await using var animation = new ItmoAnimation();
        var someRes = Result<Competition, List<HttpResponseMessage>>.Success(new Competition());

        var links = await LinksReader.Read("Scrappers/Itmo/links.txt");
        
        //Console.WriteLine("Читаем конкурсные списки ВУЗа...");
        await animation.Animate("Читаем конкурсные списки ВУЗа");

        var results = (await _itmoRequester.MakeRequests(links)).ToList();

        if (results.Any(x => !x.IsSuccessStatusCode))
        {
            return Result<Competition, List<HttpResponseMessage>>.Failure([..results.Where(x => !x.IsSuccessStatusCode)]);
        }

        var competition = new Competition();

        //Console.WriteLine("Составляем симуляцию конкурсных списков...");
        await animation.Animate("Составляем симуляцию конкурсных списков");

        var parserResults = await Task.WhenAll(from page in results select _itmoParser.CreateCompetitionList(page));

        competition.CompetitionLists.AddRange(parserResults.Select(x => x.CompetitionList));
        competition.Candidates.AddRange([..parserResults.SelectMany(x => x.Candidates)]);
        
        await animation.Stop();

        return Result<Competition, List<HttpResponseMessage>>.Success(competition);
    }
}