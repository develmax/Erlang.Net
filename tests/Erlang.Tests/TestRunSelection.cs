using System.Text.Json;

namespace Erlang.Tests;

internal sealed class TestRunSelection
{
    private readonly HashSet<string> requestedBlocks = new(StringComparer.Ordinal);
    private readonly HashSet<string> requestedCases = new(StringComparer.Ordinal);

    internal string? ReportPath { get; private set; }

    internal string? RegistryPath { get; private set; }

    internal bool ListOnly { get; private set; }

    internal bool IsPartial => requestedBlocks.Count != 0 || requestedCases.Count != 0;

    internal static TestRunSelection Parse(string[] arguments)
    {
        var selection = new TestRunSelection();
        if (!arguments.Any(value => value.StartsWith(TestRunNames.OptionPrefix, StringComparison.Ordinal)))
        {
            selection.ReportPath = arguments.FirstOrDefault();
            selection.RegistryPath = arguments.Length > 1 ? arguments[1] : null;

            return selection;
        }
        for (int index = 0; index < arguments.Length; index++)
        {
            string option = arguments[index];
            if (option == TestRunNames.List)
            {
                if (selection.ListOnly)
                    throw new ArgumentException(TestRunNames.DuplicateOption + option);
                selection.ListOnly = true;
                continue;
            }
            if (option is not (TestRunNames.Block or TestRunNames.Case or TestRunNames.Report or TestRunNames.Registry))
                throw new ArgumentException(option.StartsWith(TestRunNames.OptionPrefix, StringComparison.Ordinal)
                    ? TestRunNames.UnknownOption + option : TestRunNames.MixedArguments);
            if (++index >= arguments.Length || string.IsNullOrWhiteSpace(arguments[index])
                || arguments[index].StartsWith(TestRunNames.OptionPrefix, StringComparison.Ordinal))
                throw new ArgumentException(TestRunNames.MissingValue + option);
            string value = arguments[index];
            switch (option)
            {
                case TestRunNames.Block:
                    selection.requestedBlocks.Add(value);
                    break;
                case TestRunNames.Case:
                    selection.requestedCases.Add(value);
                    break;
                case TestRunNames.Report:
                    if (selection.ReportPath is not null)
                        throw new ArgumentException(TestRunNames.DuplicateOption + option);
                    selection.ReportPath = value;
                    break;
                case TestRunNames.Registry:
                    if (selection.RegistryPath is not null)
                        throw new ArgumentException(TestRunNames.DuplicateOption + option);
                    selection.RegistryPath = value;
                    break;
            }
        }
        if ((selection.IsPartial || selection.ListOnly) && selection.RegistryPath is not null)
            throw new ArgumentException(TestRunNames.PartialRegistry);
        if ((selection.IsPartial || selection.ListOnly) && selection.ReportPath is not null)
            RejectCanonicalReport(selection.ReportPath);

        return selection;
    }

    internal IReadOnlyList<(string Name, Func<Task> Body)> Select(
        IReadOnlyList<(string Name, Func<Task> Body)> tests,
        TestRunCatalog catalog
    )
    {
        foreach (string block in requestedBlocks)
            if (!TestRunNames.Blocks.Contains(block, StringComparer.Ordinal))
                throw new ArgumentException(TestRunNames.UnknownBlock + block);
        foreach (string name in requestedCases)
            if (!catalog.Contains(name))
                throw new ArgumentException(TestRunNames.UnknownCase + name);
        var selected = tests.Where(test => !IsPartial || requestedCases.Contains(test.Name)
            || requestedBlocks.Contains(catalog.BlockFor(test.Name))).ToArray();
        if (selected.Length == 0)
            throw new ArgumentException(TestRunNames.EmptySelection);

        return selected;
    }

    internal object Report(
        int passed,
        int failed,
        IReadOnlyCollection<object> results,
        int selected,
        int total
    )
    {
        if (!IsPartial)
            return new { Passed = passed, Failed = failed, Tests = results };

        return new
        {
            Partial = true,
            SelectedCount = selected,
            TotalCount = total,
            Blocks = requestedBlocks.Order(StringComparer.Ordinal).ToArray(),
            ExactCases = requestedCases.Order(StringComparer.Ordinal).ToArray(),
            Passed = passed,
            Failed = failed,
            Tests = results
        };
    }

    internal async Task WriteListingAsync(TestRunCatalog catalog, IEnumerable<string> selectedNames)
    {
        string[] names = selectedNames.ToArray();
        foreach (string name in names)
            Console.WriteLine($"{catalog.BlockFor(name)}\t{name}");
        Console.WriteLine($"{names.Length}/{catalog.Names.Count} listed; no test body executed");
        if (ReportPath is null)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ReportPath))!);
        await File.WriteAllTextAsync(
            ReportPath,
            JsonSerializer.Serialize(
                new
                {
                    Partial = IsPartial,
                    Listed = true,
                    SelectedCount = names.Length,
                    TotalCount = catalog.Names.Count,
                    Tests = names.Select(name => new { Name = name, Block = catalog.BlockFor(name) })
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static void RejectCanonicalReport(string report)
    {
        string full = Path.GetFullPath(report);
        string[] segments = full.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(part => part.Equals(TestRunNames.DocsSegment, StringComparison.OrdinalIgnoreCase)
            || part.Equals(TestRunNames.ReadinessSegment, StringComparison.OrdinalIgnoreCase)
            || part.Equals(TestRunNames.ToolReadinessSegment, StringComparison.OrdinalIgnoreCase))
            || TestRunNames.CanonicalReportFiles.Contains(Path.GetFileName(full), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException(TestRunNames.PartialReport);
    }
}
