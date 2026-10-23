using System.Text;

namespace SmartCareer.Web.Services.CareerCatalogImport;

public static class CareerCatalogCsv
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> Read(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var rows = Parse(reader).ToList();
        if (rows.Count == 0)
        {
            throw new InvalidDataException($"CSV file '{path}' is empty.");
        }

        var headers = rows[0];
        if (headers.Count == 0 || headers.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException($"CSV file '{path}' has an invalid header row.");
        }

        if (headers.Distinct(StringComparer.Ordinal).Count() != headers.Count)
        {
            throw new InvalidDataException($"CSV file '{path}' has duplicate headers.");
        }

        var result = new List<IReadOnlyDictionary<string, string>>(rows.Count - 1);
        for (var index = 1; index < rows.Count; index++)
        {
            if (rows[index].Count != headers.Count)
            {
                throw new InvalidDataException(
                    $"CSV file '{path}' row {index + 1} has {rows[index].Count} fields; expected {headers.Count}.");
            }

            result.Add(headers.Zip(rows[index], (header, value) => (header, value))
                .ToDictionary(pair => pair.header, pair => pair.value, StringComparer.Ordinal));
        }

        return result;
    }

    internal static IEnumerable<IReadOnlyList<string>> Parse(TextReader reader)
    {
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;

        while (reader.Read() is var value && value != -1)
        {
            var character = (char)value;
            if (quoted)
            {
                if (character == '"')
                {
                    if (reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    if (reader.Peek() == '\n')
                    {
                        reader.Read();
                    }
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row;
                    row = [];
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row;
                    row = [];
                    break;
                default:
                    field.Append(character);
                    break;
            }
        }

        if (quoted)
        {
            throw new InvalidDataException("CSV contains an unterminated quoted field.");
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            yield return row;
        }
    }
}
