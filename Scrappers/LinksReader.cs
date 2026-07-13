using System.Text.RegularExpressions;

namespace VuzScrapper.Scrappers;

public partial class LinksReader
{
    public static async Task<IEnumerable<string>> Read(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        return lines
            .Where(x => !string.IsNullOrWhiteSpace(x) && !x.Trim().StartsWith('#'))
            .Select(x => CommentRegex().Replace(x, "").Trim());
    }

    [GeneratedRegex(@"\s*#.*")]
    private static partial Regex CommentRegex();
}