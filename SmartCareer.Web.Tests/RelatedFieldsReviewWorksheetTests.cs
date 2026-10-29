using System.IO.Compression;
using System.Xml.Linq;

namespace SmartCareer.Web.Tests;

public sealed class RelatedFieldsReviewWorksheetTests
{
    [Fact]
    public void WorksheetContainsAllMappingsWithBlankReviewerFieldsAndRequiredEvidence()
    {
        var rows = ReadRows(ReviewPath());
        Assert.Equal(54, rows.Count);
        var header = rows[0];
        Assert.Equal("Mapping ID", header[0]);
        Assert.Equal("Reviewer decision", header[18]);
        Assert.Equal("Independent reviewer notes", header[25]);

        var mappings = rows.Skip(1).ToList();
        Assert.Equal(53, mappings.Count);
        Assert.Equal(53, mappings.Select(row => row[0]).Distinct(StringComparer.Ordinal).Count());
        Assert.All(mappings, row =>
        {
            Assert.Equal("In Review", row[9]);
            Assert.False(string.IsNullOrWhiteSpace(row[10]));
            Assert.False(string.IsNullOrWhiteSpace(row[11]));
            Assert.All(row.Skip(18).Take(8), Assert.Empty);
        });
    }

    [Fact]
    public void RegulatedRowsRequireIndependentReviewAndRetainRegulatorEvidence()
    {
        var mappings = ReadRows(ReviewPath()).Skip(1).ToList();
        var regulated = mappings.Where(row => row[14] == "Yes").ToList();

        Assert.Equal(15, regulated.Count);
        Assert.All(regulated, row =>
        {
            Assert.Equal("Yes", row[17]);
            Assert.StartsWith("https://", row[15], StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(row[16]));
            Assert.All(row.Skip(22).Take(4), Assert.Empty);
        });
        Assert.All(mappings.Except(regulated), row => Assert.Equal("No", row[17]));
    }

    private static IReadOnlyList<IReadOnlyList<string>> ReadRows(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var stringsEntry = archive.GetEntry("xl/sharedStrings.xml");
        var strings = stringsEntry is null
            ? []
            : XDocument.Load(stringsEntry.Open()).Descendants().Where(element => element.Name.LocalName == "si")
                .Select(item => string.Concat(item.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value))).ToList();
        var sheet = XDocument.Load(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        return sheet.Descendants().Where(element => element.Name.LocalName == "row").Select(row =>
        {
            var values = Enumerable.Repeat(string.Empty, 26).ToArray();
            foreach (var cell in row.Elements().Where(element => element.Name.LocalName == "c"))
            {
                var reference = cell.Attribute("r")!.Value;
                var index = ColumnIndex(reference);
                var type = cell.Attribute("t")?.Value;
                var raw = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "v")?.Value ?? string.Empty;
                values[index] = type == "s" && int.TryParse(raw, out var sharedIndex)
                    ? strings[sharedIndex]
                    : type == "inlineStr"
                        ? string.Concat(cell.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value))
                        : raw;
            }
            return (IReadOnlyList<string>)values;
        }).ToList();
    }

    private static int ColumnIndex(string reference)
    {
        var result = 0;
        foreach (var character in reference.TakeWhile(char.IsLetter)) result = result * 26 + character - 'A' + 1;
        return result - 1;
    }

    private static string ReviewPath()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "dataset-research", "related-fields", "review", "related-fields-human-review-v1.0.xlsx");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Related-fields review worksheet was not found.");
    }
}
