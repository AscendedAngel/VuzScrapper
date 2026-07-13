using System.Text.Json.Serialization;

namespace VuzScrapper.Scrappers.Spbgu;

internal sealed record ConfigData(string Name, string Link);
internal sealed record Block(string Html);
internal sealed record DataResponse(List<Block> Blocks);
internal sealed record Speciality(Guid Id);
internal sealed record Section([property: JsonPropertyName("title_3")] string Title, List<Speciality> Specialities);
internal sealed record SnapshotResponse(Guid Id, List<Section> Sections);
internal sealed record SectionData(Guid SnapshotId, Section Section, ApplicantType Type);