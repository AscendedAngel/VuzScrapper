namespace ITMOScrapper;

internal sealed class Competition
{
    public List<CompetitionList> CompetitionLists { get; set; } = new();

    public List<Applicant> Candidates { get; set; } = new();
}
