using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace ITMOScrapper.Scrappers;

internal sealed partial class ItmoScrapper : IRequestParser
{
    public List<HttpResponseMessage> Errors { get; set; } = [];
    private Competition? _competition = null;


    public async Task<Competition?> CreateCompetition(IEnumerable<string> links)
    {
        Console.WriteLine("Читаем конкурсные списки ВУЗа...");

        var results = (await MakeRequests(links)).ToList();
        Errors.AddRange(results.Where(x => x.StatusCode != HttpStatusCode.OK));
        if (Errors.Count != 0) return _competition;

        _competition = new Competition();
        var tasks = new List<Task<CompetitionList>>();
        var bag = new ConcurrentBag<Applicant>();

        Console.WriteLine("Составляем симуляцию конкурсных списков...");
        foreach (var page in results)
        {
            var task = Task.Run(() => CreateCompetitionList(page, bag));
            tasks.Add(task);
            await Task.Delay(500);
        }

        await Task.WhenAll(tasks);

        _competition.CompetitionLists.AddRange(tasks.Select(x => x.Result));
        _competition.Candidates.AddRange([..bag]);

        return _competition;
    }

    #region Private Implementation

    private async Task<IEnumerable<HttpResponseMessage>> MakeRequests(IEnumerable<string> links) 
    {
        var client = new HttpClient();
        var tasks = new List<Task<HttpResponseMessage>>();

        foreach (var link in links) 
        {
            tasks.Add(client.GetAsync(link));
        }

        await Task.WhenAll(tasks);

        return tasks.Select(x => x.Result);
    }

    private static CompetitionList CreateCompetitionList(HttpResponseMessage response, ConcurrentBag<Applicant> candidates) 
    {
        var content = response.Content.ReadAsStringAsync().Result;

        var parser = new HtmlParser();
        var document = parser.ParseDocument(content);

        var fullData = document.QuerySelector("div[class^=RatingPage_rating__]")!;
        var programName = fullData.QuerySelector("h2")!.TextContent;

        var infoBlock = fullData.QuerySelector("div:first-child")!;

        var infoText = infoBlock.QuerySelector("p")!.TextContent;

        var competitionList = new CompetitionList
        {
            Name = programName
        };

        FillPlacesInfo(competitionList, infoText);

        var subcompetitions = fullData.QuerySelectorAll(":scope > div[class^=RatingPage_table__]").ToArray();

        var offset = programName.StartsWith("01.03.02") ? 0 : 1;
        var olympiads = subcompetitions[0];
        var targets = subcompetitions[offset];
        var specials = subcompetitions[1 + offset];
        var separates = subcompetitions[2 + offset];
        var common = subcompetitions[3 + offset];

        FillCandidates(olympiads, programName, candidates, ApplicantType.Olympiad);
        if (offset == 1) FillCandidates(targets, programName, candidates, ApplicantType.Target);
        FillCandidates(specials, programName, candidates, ApplicantType.Special);
        FillCandidates(separates, programName, candidates, ApplicantType.Separate);
        FillCandidates(common, programName, candidates, ApplicantType.Common);

        return competitionList;
    }

    private static void FillPlacesInfo(CompetitionList competitionList, string infoText) 
    {
        var regexResults = PlacesRegex().Matches(infoText);

        foreach (var regexResult in regexResults) 
        {
            var groups = ((Match)regexResult).Groups;

            if (string.IsNullOrEmpty(groups[^1].Value)) competitionList.Places = int.Parse(groups[1].Value);
            if (groups[^1].Value == "ЦК") competitionList.TargetedQuota = int.Parse(groups[1].Value);
            if (groups[^1].Value == "ОcК") competitionList.SpecialQuota = int.Parse(groups[1].Value);
            if (groups[^1].Value == "ОтК") competitionList.SeparateQuota = int.Parse(groups[1].Value);
        }
    }

    private static void FillCandidates(IElement element, string competitionListName, ConcurrentBag<Applicant> candidates, ApplicantType applicantType = ApplicantType.Common)
    {
        foreach (var target in element.QuerySelectorAll("div[class^=RatingPage_table__item__]"))
        {
            var code = CodeRegex().Match(target.TextContent).Groups[1].Value;
            var priority = int.Parse(PriorityRegex().Match(target.TextContent).Groups[1].Value);

            var individual = applicantType == ApplicantType.Olympiad ? 0 : int.Parse(IndividualRegex().Match(target.TextContent).Groups[1].Value);
            var exam = applicantType == ApplicantType.Olympiad ? int.MaxValue : int.Parse(ExamRegex().Match(target.TextContent).Groups[1].Value);
            var score = individual + exam;
            
            var applicant = new Applicant 
            {
                Code = code,
                Program = competitionListName,
                ProgramPriority = priority,
                Scores = score,
                ApplicantType = applicantType
            };

            candidates.Add(applicant);
        }
    }

    #endregion

    #region Regexes

    [GeneratedRegex(@"(\d+)\s([а-яА-Яa-zA-Z]+)?")]
    private static partial Regex PlacesRegex();

    [GeneratedRegex(@"№(\d+)")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"Приоритет:\s(\d)")]
    private static partial Regex PriorityRegex();

    [GeneratedRegex(@"ИД:\s(\d+)")]
    private static partial Regex IndividualRegex();

    [GeneratedRegex(@"Балл\sВИ:\s(\d+)")]
    private static partial Regex ExamRegex();
    
    #endregion

}