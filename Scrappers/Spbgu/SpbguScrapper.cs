using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html;
using AngleSharp.Html.Parser;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed class SpbguScrapper : IRequestParser
{
    #region JSON Types

    private sealed record ConfigData
    {
        public string Name { get; set; } = null!;
        public string Link { get; set; } = null!;
    }
    
    private sealed record Block
    {
        public string Html { get; set; }
    }
    
    private sealed record DataResponse
    {
        public List<Block> Blocks { get; set; }
    }
    
    
    private sealed record Speciality
    {
        public Guid Id { get; set; }
    }
    
    private sealed record Section
    {
        [JsonPropertyName("title_3")]
        public string Title { get; set; }
        public List<Speciality> Specialities { get; set; }
    }
    
    private sealed record SnapshotResponse
    {
        public Guid Id { get; set; }
        public List<Section> Sections { get; set; } 
    }

    private sealed record SectionData
    {
        public Section Section { get; set; } = null!;
        public ApplicantType Type { get; set; }
    }

    #endregion

    public string Name => "ФГБОУ ВО \"СПбГУ\"";
    
    public List<HttpResponseMessage> Errors { get; set; } = [];

    private HttpClient _client = null!;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private readonly HtmlParser _parser = new();

    // Returns: (string - program name, int - places count, ApplicantType - type of group)
    private async Task<(string, int, ApplicantType)?> ProceedCompetitionListGroup(Guid snapshotId, SectionData sectionData, ConcurrentBag<Applicant> applicants)
    {
        const string dataLink = "https://enrollelists.spbu.ru/api/reports/priem-list-02/data";
        
        var reqObj = new
        {
            report_priem_list_02_id = snapshotId,
            speciality_ids = sectionData.Section.Specialities.Select(x => x.Id).ToList()
        };
        
        var apiResult = await _client.PostAsJsonAsync(dataLink, reqObj);
        if (!apiResult.IsSuccessStatusCode)
        {
            Errors.Add(apiResult);
            return null;
        }
        
        var dataResponse = (await JsonSerializer.DeserializeAsync<DataResponse>(await apiResult.Content.ReadAsStreamAsync(), _jsonOptions))!;
        var dataDom = await _parser.ParseDocumentAsync(dataResponse.Blocks.First().Html);
        var programCode = dataDom.QuerySelector(".table-information > table > tbody > tr:nth-child(2) > td > span");
        var programName = dataDom.QuerySelector(".table-information > table > tbody > tr:nth-child(3) > td");
        var places = int.Parse(dataDom.QuerySelector(".table-information tr:last-child > td")!.TextContent);
        var rows = dataDom.QuerySelectorAll(".table-data tbody > tr").ToList();

        var program = $"{programCode!.TextContent} {programName!.TextContent}";

        var applicantsLinq =
            from row in rows
            select row.QuerySelectorAll("td").ToList()
            into tds
            let code = tds.ElementAt(1).TextContent
            let scoresContent = tds.ElementAt(2).TextContent
            let scores = scoresContent.Contains("БВИ") ? int.MaxValue : int.Parse(scoresContent)
            let priority = int.Parse(tds.ElementAt(11).TextContent)
            select new Applicant
            {
                Code = code,
                Program = program,
                ProgramPriority = priority,
                Scores = scores,
                ApplicantType = scores == int.MaxValue && sectionData.Type == ApplicantType.Common ? ApplicantType.Olympiad : sectionData.Type
            };
        
        foreach (var applicant in applicantsLinq)
        {
            applicants.Add(applicant);
        }

        return (program, places, sectionData.Type);
    }

    private async Task<CompetitionList?> ProceedCompetitionList(string link, ConcurrentBag<Applicant> applicants)
    {
        const string budgetTitle = "контрольных цифр";
        const string specialTitle = "особой квоты";
        const string separateTitle = "отдельной квоты";
        
        var competitionList = new CompetitionList();
        
        var request = await _client.GetAsync(link);
        if (!request.IsSuccessStatusCode)
        {
            Errors.Add(request);
            return null;
        }
        
        var parser = new HtmlParser();
        var snapshotDom = await parser.ParseDocumentAsync(await request.Content.ReadAsStringAsync());
        var snapshotDataElement = snapshotDom.QuerySelector("#priem-list-02-report-meta")!;
        
        var snapshotData = (await Task.Run(() => JsonSerializer.Deserialize<SnapshotResponse>(snapshotDataElement.TextContent, _jsonOptions)))!;
        
        var snapshotId = snapshotData.Id;
        var budget = snapshotData.Sections.First(x => x.Title.Contains(budgetTitle));
        var special = snapshotData.Sections.First(x => x.Title.Contains(specialTitle));
        var separate = snapshotData.Sections.First(x => x.Title.Contains(separateTitle));

        var sections = new[]
        {
            new SectionData { Section = budget, Type = ApplicantType.Common },
            new SectionData { Section = special, Type = ApplicantType.Special },
            new SectionData { Section = separate, Type = ApplicantType.Separate }
        };

        var tasks = new List<Task<(string, int, ApplicantType)?>>();

        foreach (var section in sections)
        {
            tasks.Add(ProceedCompetitionListGroup(snapshotId, section, applicants));
            await Task.Delay(500);
        }

        await Task.WhenAll(tasks);
        var results = tasks.Select(x => x.Result).ToArray();

        if (results.Any(x => x is null))
        {
            return null;
        }

        competitionList.Name = results.First()!.Value.Item1;
        competitionList.TargetedQuota = results.FirstOrDefault(x => x!.Value.Item3 == ApplicantType.Target)?.Item2 ?? 0;
        competitionList.SpecialQuota = results.First(x => x!.Value.Item3 == ApplicantType.Special)!.Value.Item2;
        competitionList.SeparateQuota = results.First(x => x!.Value.Item3 == ApplicantType.Separate)!.Value.Item2;
        competitionList.Places = competitionList.TargetedQuota + competitionList.SpecialQuota + competitionList.SeparateQuota + results.First(x => x!.Value.Item3 == ApplicantType.Common)!.Value.Item2;

        return competitionList;
    }

    public async Task<Competition?> CreateCompetition()
    {
        using (_client = new HttpClient())
        {
            var links = await LinksReader.Read("Scrappers/Spbgu/links.txt");

            var competition = new Competition();
            var candidatesBag = new ConcurrentBag<Applicant>();

            var tasks = new List<Task<CompetitionList?>>();
        
            foreach (var data in links)
            {
                tasks.Add(ProceedCompetitionList(data, candidatesBag));
                await Task.Delay(500);
            }
            await Task.WhenAll(tasks);
            if (Errors.Count != 0)
            {
                return null;
            }

            competition.CompetitionLists = [..tasks.Select(x => x.Result)!];
            competition.Candidates = candidatesBag.ToList();

            return competition;
        }
    }
}