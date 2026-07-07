namespace VuzScrapper;

interface IRequestParser
{
    public List<HttpResponseMessage> Errors { get; set; }

    public Task<Competition?> CreateCompetition(IEnumerable<string> links);
}