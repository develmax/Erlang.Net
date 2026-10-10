using System.Text;
using System.Text.Json;
using Erlang;

internal static class Readiness
{
    private sealed record Export(string Name, int Arity);
    private sealed record SourceModule(
        string Name,
        string Application,
        string Source,
        Export[] Exports,
        bool RequiresPreprocessing
    );
    private sealed record SourceInventory(
        string Tag,
        string Commit,
        SourceModule[] Modules,
        Export[] Bifs
    );
    private sealed record Baseline(string Tag, string Commit);
    private sealed record Mfa(
        string Module,
        string Function,
        int Arity,
        string Evidence
    );
    private sealed record TestResult(string Name, string Status);
    private sealed record TestReport(int Passed, int Failed, TestResult[] Tests);
    private sealed record Component(
        string Id,
        string Project,
        string Status,
        string[] Implemented,
        string[] Remaining,
        string[] TestPrefixes
    );
    private sealed record Scope(string ComparisonTestReport, Component[] Components, string[] FocusModules);

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    private static T Load<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)!;

    public static async Task Generate(string docs, string tests, string output)
    {
        var inventory = Load<SourceInventory>(Path.Combine(docs, ReadinessText.InventoryFile));
        var baseline = Load<Baseline>(Path.Combine(docs, "..", ReadinessText.BaselineFile));
        var registered = Load<Mfa[]>(Path.Combine(docs, ReadinessText.RegistryFile));
        var report = Load<TestReport>(tests);
        var scope = Load<Scope>(Path.Combine(docs, ReadinessText.ScopeFile));
        var previous = Load<TestReport>(Path.Combine(docs, scope.ComparisonTestReport));
        if (report.Failed != 0 || report.Tests.Any(test => test.Status != ReadinessText.Passed) || report.Passed != report.Tests.Length || inventory.Tag != baseline.Tag || inventory.Commit != baseline.Commit)
            throw new InvalidOperationException(ToolDiagnostics.ReadinessInvalidInput);

        var runtime = new ModuleRegistry();
        CoreModules.Register(runtime);
        Erlang.Otp.OtpModules.Register(runtime);
        var actual = runtime.Exports.Select(export => Key(export.Module, export.Function, export.Arity)).ToHashSet();
        var declared = registered.Select(export => Key(export.Module, export.Function, export.Arity)).ToHashSet();
        if (registered.Length != declared.Count || !actual.SetEquals(declared) || registered.Any(export => export.Evidence != ReadinessText.MfaTestPrefix + Key(export.Module, export.Function, export.Arity) || !report.Tests.Any(test => test.Name == export.Evidence && test.Status == ReadinessText.Passed)))
            throw new InvalidOperationException(ToolDiagnostics.ReadinessRegistryMismatch);

        var previousMfas = previous.Tests.Where(test => test.Status == ReadinessText.Passed && test.Name.StartsWith(ReadinessText.MfaTestPrefix, StringComparison.Ordinal)).Select(test => test.Name[ReadinessText.MfaTestPrefix.Length..]).ToHashSet();
        var references = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var module in inventory.Modules)
        {
            if (!references.TryGetValue(module.Name, out var exports))
                references[module.Name] = exports = [];
            foreach (var export in module.Exports)
                exports.Add(Key(module.Name, export.Name, export.Arity));
        }
        foreach (var bif in inventory.Bifs)
        {
            string module = bif.Name[..bif.Name.IndexOf(':')];
            if (!references.TryGetValue(module, out var exports))
                references[module] = exports = [];
            exports.Add(bif.Name + "/" + bif.Arity);
        }

        var modules = references.Keys.Concat(registered.Select(export => export.Module)).Distinct().Order(StringComparer.Ordinal).Select(name =>
        {
            var mfAs = registered.Where(export => export.Module == name).Select(export => Key(export.Module, export.Function, export.Arity)).Order(StringComparer.Ordinal).ToArray();
            int referenceCount = references.GetValueOrDefault(name)?.Count ?? 0;
            var absent = mfAs.Where(mfa => !references.GetValueOrDefault(name, []).Contains(mfa)).ToArray();

            return new
            {
                Name = name,
                Status = mfAs.Length > 0 ? ReadinessText.Partial : ReadinessText.NotStarted,
                RegisteredMfas = mfAs.Length,
                PreviousRegisteredMfas = previousMfas.Count(mfa => mfa.StartsWith(name + ":", StringComparison.Ordinal)),
                Delta = mfAs.Length - previousMfas.Count(mfa => mfa.StartsWith(name + ":", StringComparison.Ordinal)),
                ReferenceDeclarations = referenceCount,
                ApiPresencePercent = referenceCount == 0 || absent.Length > 0 ? (decimal?)null : Math.Round(100m * mfAs.Length / referenceCount, 1),
                RequiresPreprocessing = inventory.Modules.Any(module => module.Name == name && module.RequiresPreprocessing),
                Sources = inventory.Modules.Where(module => module.Name == name).Select(module => module.Source).ToArray(),
                Mfas = mfAs,
                AbsentFromReference = absent
            };
        }).ToArray();

        var components = scope.Components.Select(component => new
        {
            component.Id,
            component.Project,
            component.Status,
            component.Implemented,
            component.Remaining,
            RelatedPassedTests = report.Tests.Count(test => component.TestPrefixes.Any(prefix => test.Name.StartsWith(prefix, StringComparison.Ordinal)))
        }).ToArray();
        var data = new
        {
            baseline.Tag,
            baseline.Commit,
            Methodology = ReadinessText.Method,
            ComparisonTestReport = scope.ComparisonTestReport,
            LocalPassedTests = report.Passed,
            RegisteredMfas = registered.Length,
            PreviousRegisteredMfas = previousMfas.Count,
            RegisteredMfaDelta = registered.Length - previousMfas.Count,
            SourceModuleRows = inventory.Modules.Length,
            ImplementedApiModules = modules.Count(module => module.RegisteredMfas > 0),
            Components = components,
            Modules = modules
        };
        var markdown = new StringBuilder().AppendLine(ReadinessText.Title).AppendLine().AppendLine(ReadinessText.Summary(
            baseline.Tag,
            report.Passed,
            registered.Length,
            previousMfas.Count,
            data.ImplementedApiModules,
            inventory.Modules.Length
        )).AppendLine().AppendLine(ReadinessText.Method).AppendLine().AppendLine(ReadinessText.ComponentHeading).AppendLine().AppendLine(ReadinessText.ComponentHeader).AppendLine(ReadinessText.ComponentSeparator);
        foreach (var component in components)
            markdown.AppendLine(ReadinessText.ComponentRow(
                component.Id,
                component.Status,
                component.RelatedPassedTests,
                component.Implemented,
                component.Remaining
            ));
        markdown.AppendLine().AppendLine(ReadinessText.ModuleHeading).AppendLine().AppendLine(ReadinessText.ModuleHeader).AppendLine(ReadinessText.ModuleSeparator);
        foreach (var module in modules.Where(module => module.RegisteredMfas > 0 || scope.FocusModules.Contains(module.Name)))
            markdown.AppendLine(ReadinessText.ModuleRow(
                module.Name,
                module.RegisteredMfas,
                module.Delta,
                module.ReferenceDeclarations,
                module.ApiPresencePercent,
                module.Status
            ));
        markdown.AppendLine().AppendLine(ReadinessText.FacadeBoundary);
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, ReadinessText.ReportJson), JsonSerializer.Serialize(data, JsonOptions) + "\n", new UTF8Encoding(false));
        await File.WriteAllTextAsync(Path.Combine(output, ReadinessText.ReportMarkdown), markdown.ToString().Replace("\r\n", "\n"), new UTF8Encoding(false));
    }

    private static string Key(string module, string function, int arity) => module + ":" + function + "/" + arity;
}
