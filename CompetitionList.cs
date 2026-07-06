namespace ITMOScrapper;

internal sealed class CompetitionList 
{
    public string Name { get; set; } = null!;
    public int Places { get; set; }
    public int TargetedQuota { get; set; }
    public int SpecialQuota { get; set; }
    public int SeparateQuota { get; set; }

    public List<Applicant> Students { get; set; } = new();

}
