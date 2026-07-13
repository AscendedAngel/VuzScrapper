using System.Text.Json;
using System.Text.Json.Serialization;
using AngleSharp.Html.Parser;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed class SpbguParser
{
    private const string BudgetTitle = "контрольных цифр";
    private const string SpecialTitle = "особой квоты";
    private const string SeparateTitle = "отдельной квоты";

    private readonly HtmlParser _parser = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<IEnumerable<SectionData>> ParseSections(Stream stream)
    {
        var snapshotDom = await _parser.ParseDocumentAsync(stream);
        var snapshotDataElement = snapshotDom.QuerySelector("#priem-list-02-report-meta")!;
        var snapshotData = JsonSerializer.Deserialize<SnapshotResponse>(snapshotDataElement.TextContent, _jsonOptions)!;

        var snapshotId = snapshotData.Id;
        var budget = snapshotData.Sections.First(x => x.Title.Contains(BudgetTitle));
        var special = snapshotData.Sections.First(x => x.Title.Contains(SpecialTitle));
        var separate = snapshotData.Sections.First(x => x.Title.Contains(SeparateTitle));

        return
        [
            new SectionData(snapshotId, budget, ApplicantType.Common),
            new SectionData(snapshotId, special, ApplicantType.Special),
            new SectionData(snapshotId, separate, ApplicantType.Separate)
        ];
    }

    public async Task<(IEnumerable<Applicant> Applicants, string Program, int Places)> ParseApplicants(Stream stream, ApplicantType sectionType)
    {
        var dataResponse = (await JsonSerializer.DeserializeAsync<DataResponse>(stream, _jsonOptions))!;
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
                ApplicantType = scores == int.MaxValue && sectionType == ApplicantType.Common ? ApplicantType.Olympiad : sectionType
            };

        return (applicantsLinq, program, places);
    }
}