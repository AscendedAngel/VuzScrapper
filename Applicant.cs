namespace ITMOScrapper;

internal enum ApplicantType
{
    Olympiad,
    Target,
    Special,
    Separate,
    Common
}

internal class Applicant
{
    public string Code { get; set; } = null!;
    public string Program { get; set; } = null!;
    public int ProgramPriority { get; set; }
    public int Scores { get; set; }

    public ApplicantType ApplicantType { get; set; } = ApplicantType.Common;

    public override string ToString()
    {
        return $"{Code} - {Program} - {ProgramPriority} - {ApplicantType} - {Scores}";
    }
}
