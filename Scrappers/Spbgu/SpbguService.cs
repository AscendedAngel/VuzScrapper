using System.Collections.Concurrent;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed class SpbguService(SpbguRequester requester, SpbguParser parser)
{
    // Returns: (string - program name, int - places count, ApplicantType - type of group)
    private async Task<Result<(List<Applicant> Applicants, string Program, int Places, ApplicantType Type), HttpResponseMessage>> ProceedCompetitionListGroup(SectionData sectionData)
    {
        var reqObj = new
        {
            report_priem_list_02_id = sectionData.SnapshotId,
            speciality_ids = sectionData.Section.Specialities.Select(x => x.Id).ToList()
        };
        
        var requestResult = await requester.FetchApplicantDataStream(reqObj);
        if (requestResult.IsFailure)
        {
            return Result<(List<Applicant>, string, int, ApplicantType), HttpResponseMessage>.Failure(requestResult.Error);
        }
        
        var parsedApplicants = await parser.ParseApplicants(requestResult.Value, sectionData.Type);

        return Result<(List<Applicant>, string, int, ApplicantType), HttpResponseMessage>.Success(([..parsedApplicants.Applicants], parsedApplicants.Program, parsedApplicants.Places, sectionData.Type));
    }

    public async Task<Result<(CompetitionList CompetitionList, List<List<Applicant>> Lists), List<HttpResponseMessage>>> ProceedCompetitionList(string link)
    {   
        var competitionList = new CompetitionList();
        
        var streamResult = await requester.FetchSnapshotStream(link);
        if (streamResult.IsFailure)
        {
            return Result<(CompetitionList, List<List<Applicant>>), List<HttpResponseMessage>>.Failure([streamResult.Error]);
        }

        var sections = await parser.ParseSections(streamResult.Value);
        var results = await Task.WhenAll(from section in sections select ProceedCompetitionListGroup(section));

        if (results.Any(x => x.IsFailure))
        {
            var fails =
                from result in results
                where result.IsFailure
                select result.Error;

            return Result<(CompetitionList, List<List<Applicant>>), List<HttpResponseMessage>>.Failure([..fails]);
        }

        var resultsData = results.Select(x => x.Value!);

        competitionList.Name = resultsData.First()!.Program;
        competitionList.TargetedQuota = resultsData.FirstOrDefault(x => x!.Type == ApplicantType.Target).Places;
        competitionList.SpecialQuota = resultsData.First(x => x!.Type == ApplicantType.Special).Places;
        competitionList.SeparateQuota = resultsData.First(x => x!.Type == ApplicantType.Separate).Places;
        competitionList.Places = competitionList.TargetedQuota + competitionList.SpecialQuota + competitionList.SeparateQuota + resultsData.First(x => x.Type == ApplicantType.Common).Places;

        var lists = resultsData.Select(x => x.Applicants);

        return Result<(CompetitionList, List<List<Applicant>>), List<HttpResponseMessage>>.Success((competitionList, [..lists]));
    }
}