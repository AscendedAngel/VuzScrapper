using System.Collections.Concurrent;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed class SpbguScrapper : IRequestParser
{
    public string Name => "ФГБОУ ВО \"СПбГУ\"";
    
    private readonly SpbguParser _parser;
    private readonly SpbguRequester _requester;
    private readonly SpbguService _unnamed;

    public SpbguScrapper(HttpClientWrapper client)
    {
        _parser = new();
        _requester = new(client);
        _unnamed = new(_requester, _parser);
    }

    public async Task<Result<Competition, List<HttpResponseMessage>>> CreateCompetition()
    {
        
        var links = await LinksReader.Read("Scrappers/Spbgu/links.txt");

        var competition = new Competition();

        Console.WriteLine("Читаем конкурсные списки ВУЗа...");

        var results = await Task.WhenAll(from link in links select _unnamed.ProceedCompetitionList(link));
        if (results.Any(x => x.IsFailure))
        {
            var fails =
                from result in results
                where result.IsFailure
                from error in result.Error!
                select error;
            return Result<Competition, List<HttpResponseMessage>>.Failure([..fails]);
        }

        var resultsData = results.Select(x => x.Value);

        Console.WriteLine("Составляем симуляцию конкурсных списков...");

        competition.CompetitionLists = [..resultsData.Select(x => x.CompetitionList)];
        var candidates = resultsData.SelectMany(x => x.Lists).SelectMany(x => x);

        competition.Candidates = [..candidates];

        return Result<Competition, List<HttpResponseMessage>>.Success(competition);
    }
}