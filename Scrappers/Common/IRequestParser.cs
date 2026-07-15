namespace VuzScrapper.Scrappers.Common;

interface IRequestParser
{
    public string Name { get; }

    public Task<Result<Competition, List<HttpResponseMessage>>> CreateCompetition();
}