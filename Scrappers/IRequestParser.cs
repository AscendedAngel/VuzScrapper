namespace VuzScrapper.Scrappers;

interface IRequestParser
{
    public string Name { get; }
    
    public List<HttpResponseMessage> Errors { get; set; }

    public Task<Competition?> CreateCompetition();
}