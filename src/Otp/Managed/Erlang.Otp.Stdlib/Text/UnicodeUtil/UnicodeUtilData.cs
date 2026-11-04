using System.IO.Compression;
using System.Text.Json;
using static Erlang.UnicodeUtilDataFormat;

namespace Erlang;

// Derived from the OTP29.1.1 Unicode17.0 generator, Apache-2.0.
// Copyright Ericsson AB 2017-2026. Data provenance and Unicode notices are retained.
internal sealed class UnicodeUtilData
{
    internal readonly record struct Entry(int Combining, Term Canonical, Term Compatibility);
    private readonly Dictionary<int, Entry> entries = new();
    private readonly Dictionary<int, Term[]> cases = new();
    private readonly Dictionary<long, int> pairs = new();
    private readonly Dictionary<string, (int First, int Last)[]> properties = new(StringComparer.Ordinal);
    private readonly (int First, int Last, Term Category)[] categories;
    internal static UnicodeUtilData Instance { get; } = new();

    private UnicodeUtilData()
    {
        using Stream resource = typeof(UnicodeUtilData).Assembly.GetManifestResourceStream(UnicodeUtilNames.DataResource)!;
        using var gzip = new GZipStream(resource, CompressionMode.Decompress);
        using JsonDocument document = JsonDocument.Parse(gzip);
        JsonElement root = document.RootElement;
        if (root.GetProperty("SourceSha256").GetString() != UnicodeUtilNames.PinnedSourceSha256
            || root.GetProperty("Version")[0].GetInt32() != UnicodeUtilNames.VersionMajor
            || root.GetProperty("Version")[1].GetInt32() != UnicodeUtilNames.VersionMinor)
            throw new InvalidDataException(UnicodeUtilNames.InvalidData);
        categories = root.GetProperty("Categories").EnumerateArray().Select(row => (
            row[RangeFirst].GetInt32(), row[RangeLast].GetInt32(), (Term)Term.Tuple(Term.A(row[CategoryMajor].GetString()!), Term.A(row[CategoryMinor].GetString()!)))).ToArray();
        foreach (JsonElement row in root.GetProperty("Entries").EnumerateArray())
        {
            Term canonical = Tagged(row[EntryCanonical]);
            JsonElement compat = row[EntryCompatibility];
            Term compatibility = compat.GetArrayLength() == 0 ? Nil.Value : Term.Tuple(Term.A(compat[CompatibilityTag].GetString()!), Tagged(compat[CompatibilityMapping]));
            entries.Add(row[EntryCodepoint].GetInt32(), new Entry(row[EntryCombining].GetInt32(), canonical, compatibility));
        }
        foreach (JsonElement row in root.GetProperty("Cases").EnumerateArray())
        {
            cases.Add(
                row[CaseCodepoint].GetInt32(),
                [Mapping(row[CaseUpper]), Mapping(row[CaseLower]), Mapping(row[CaseTitle]), Mapping(row[CaseFold])]
            );
        }
        foreach (JsonElement row in root.GetProperty("Pairs").EnumerateArray())
        {
            pairs.Add(PairKey(row[PairLeft].GetInt32(), row[PairRight].GetInt32()), row[PairResult].GetInt32());
        }
        foreach (JsonProperty property in root.GetProperty("Properties").EnumerateObject())
        {
            var ranges = property.Value.EnumerateArray().Select(row => (First: row[RangeFirst].GetInt32(), Last: row[RangeLast].GetInt32())).ToArray();
            int previous = -1;
            foreach (var range in ranges)
            {
                if (range.First <= previous || range.Last < range.First || range.Last > UnicodeUtilNames.Maximum)
                    throw new InvalidDataException(UnicodeUtilNames.InvalidData);
                previous = range.Last;
            }
            properties.Add(property.Name, ranges);
        }
    }

    internal Entry Lookup(int cp) => entries.GetValueOrDefault(cp, new Entry(0, Nil.Value, Nil.Value));

    internal Term Category(int cp)
    {
        int low = 0;
        int high = categories.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            var row = categories[middle];
            if (cp < row.First) high = middle - 1;
            else if (cp > row.Last) low = middle + 1;
            else return row.Category;
        }

        throw new InvalidDataException(UnicodeUtilNames.InvalidData);
    }

    internal Term Case(int cp, int index) => cases.TryGetValue(cp, out Term[]? row) ? row[index] : Term.I(cp);

    internal bool Contains(string property, int cp)
    {
        var ranges = properties[property];
        int low = 0;
        int high = ranges.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            var row = ranges[middle];
            if (cp < row.First) high = middle - 1;
            else if (cp > row.Last) low = middle + 1;
            else return true;
        }

        return false;
    }

    internal bool Compose(int left, int right, out int composed) => pairs.TryGetValue(PairKey(left, right), out composed);

    private static long PairKey(int left, int right) => ((long)left << PairKeyShift) | (uint)right;

    private static Term Mapping(JsonElement value) => value.ValueKind == JsonValueKind.Number ? Term.I(value.GetInt32())
        : Cons.From(value.EnumerateArray().Select(cp => Term.I(cp.GetInt32())));

    private static Term Tagged(JsonElement value) => Cons.From(value.EnumerateArray().Select(row =>
        Term.Tuple(Term.I(row[TaggedClass].GetInt32()), Term.I(row[TaggedCodepoint].GetInt32()))));
}
