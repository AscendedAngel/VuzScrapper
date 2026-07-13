namespace VuzScrapper;

internal static class CompetitionResolver 
{
    private class ApplicantGroupByType
    {
        public ApplicantType ApplicantType { get; set; }
        public List<ApplicantGroupByCode> CandidatesByType { get; set; } = null!;
    }

    private class ApplicantGroupByCode
    {
        public string Code { get; set; } = null!;
        public List<Applicant> Applicants { get; set; } = [];
    }

    private static void Recycle(List<ApplicantGroupByType> groups, Competition competition, Applicant recycleFor)
    {
        var ignoreId = new List<string>();

        foreach (var block in groups)
        {
            foreach (var candidate in block.CandidatesByType)
            {
                if (ignoreId.Contains(candidate.Code)) continue;
                
                var isAdded = false;

                foreach (var request in candidate.Applicants)
                {
                    var currentList = competition.CompetitionLists.First(x => x.Name == request.Program);

                    var predict = request.ApplicantType switch 
                    {
                        ApplicantType.Olympiad => currentList.Students.Count + 1 <= currentList.Places,
                        ApplicantType.Target => currentList.Students.Count(x => x.ApplicantType == ApplicantType.Target) + 1 <= currentList.TargetedQuota,
                        ApplicantType.Special => currentList.Students.Count(x => x.ApplicantType == ApplicantType.Special) + 1 <= currentList.SpecialQuota,
                        ApplicantType.Separate => currentList.Students.Count(x => x.ApplicantType == ApplicantType.Separate) + 1 <= currentList.SeparateQuota,
                        ApplicantType.Common => currentList.Students.Count + 1 <= currentList.Places,
                        _ => false
                    };
                        
                    if (predict)
                    {
                        currentList.Students.Add(request);
                        //competition.Candidates.RemoveAll(x => x.Code == candidate.Code);
                        isAdded = true;
                        ignoreId.Add(candidate.Code);
                        break;
                    }
                }

                if (isAdded) continue;

                foreach (var request in candidate.Applicants)
                {
                    if (request.ApplicantType == ApplicantType.Target && request.Code != recycleFor.Code) continue;
                    if (request.ApplicantType == ApplicantType.Special && request.Code != recycleFor.Code) continue;
                    if (request.ApplicantType == ApplicantType.Separate && request.Code != recycleFor.Code) continue;
                    var currentList = competition.CompetitionLists.First(x => x.Name == request.Program);
                    currentList.Students.Add(request);
                }

                //competition.Candidates.RemoveAll(x => x.Code == candidate.Code);
                ignoreId.Add(candidate.Code);
            }
        }
    }

    public static void Resolve(Competition competition, Applicant recycleFor) 
    {
        var result = competition.Candidates
            .OrderBy(x => x.ApplicantType)
            .GroupBy(x => x.ApplicantType)
            .Select(x => new ApplicantGroupByType
            { 
                ApplicantType = x.Key, 
                CandidatesByType = x
                    .OrderByDescending(y => y.Scores)
                    .GroupBy(y => y.Code)
                    .Select(y => new ApplicantGroupByCode
                    {
                        Code = y.Key,
                        Applicants = y.OrderBy(z => z.ProgramPriority).ToList()
                    }).ToList()
            }).ToList();

        Recycle(result, competition, recycleFor);
    }
}
