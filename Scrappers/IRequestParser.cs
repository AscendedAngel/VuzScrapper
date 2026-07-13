namespace VuzScrapper.Scrappers;

interface IRequestParser
{
    public string Name { get; }

    public Task<Result<Competition, List<HttpResponseMessage>>> CreateCompetition();
}