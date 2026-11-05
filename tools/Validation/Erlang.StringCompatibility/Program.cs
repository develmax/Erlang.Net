using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length is not (3 or 4))
{
    Console.Error.WriteLine(StringCompatibilityDiagnostics.Usage);

    return 2;
}

string repository = Path.GetFullPath(args[0]);
string directory = Path.GetFullPath(args[1]);
bool managed = args[2] == StringCompatibilityNames.Managed;
if (!managed && args[2] != StringCompatibilityNames.Beam)
{
    throw new ArgumentException(StringCompatibilityDiagnostics.UnknownBackend);
}
Term[] declared = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf")))).ToArray();
var expectedExports = declared.Cast<TupleTerm>().Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value)).ToHashSet();
if (!expectedExports.SetEquals(StringModule.Exports))
{
    throw new InvalidOperationException(StringCompatibilityDiagnostics.ExportMismatch);
}

Console.WriteLine($"{args[2]}: preparing explicit original Unicode dependencies");
string foundation = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
byte[] Original(string name) => File.ReadAllBytes(Path.Combine(foundation, name + ".beam"));
string[] stringDependencies = ["erlang", "lists", "unicode", "unicode_util"];
var choices = new RuntimeLibrarySelection([
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("lists"),
    RuntimeModuleSelection.Beam("unicode", Original("unicode"), ["lists", "unicode_util"]),
    RuntimeModuleSelection.Beam("unicode_util", Original("unicode_util"), ["lists"]),
    managed ? RuntimeModuleSelection.Managed("string", stringDependencies)
        : RuntimeModuleSelection.Beam("string", Original("string"), stringDependencies)
]);
await using var runtime = await ProcessRuntime.CreateAsync(choices, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
byte[] originalBytes = File.ReadAllBytes(Path.Combine(directory, "original.etf"));
var decoding = new EtfDecodeOptions
{
    ExternalFunctionFactory = (identity, safe) => runtime.Functions.External(runtime, identity, safe)
};
EtfDecodedTerm decoded = ExternalTermFormat.DecodeOne(originalBytes, decoding);
if (decoded.Used != originalBytes.Length)
{
    throw new InvalidDataException(StringCompatibilityDiagnostics.TrailingData);
}
Term[] rows = Cons.Items(decoded.Term).ToArray();
int? requestedDepth = args.Length == 4 ? int.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture) : null;
{
    ModuleRegistry registry = runtime.Modules;
    var dependencies = new BeamDependencySelection(["string", "lists", "binary", "erlang", "file", "filename", "unicode", StringCompatibilityNames.Observer]);
    var prepared = registry.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, "string_port_cases.beam")), dependencies);
    await runtime.InitializeModulesAsync(registry =>
    {
        if (registry.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(StringCompatibilityDiagnostics.PublicationRefused);
        }
    });
}

Console.WriteLine($"{args[2]}: executing {rows.Length} cases");

var observations = new List<Term>();
var failures = new List<object>();
var stackFailures = new List<object>();
int values = 0;
int errors = 0;
int stackMatches = 0;
int rawStackMatches = 0;
int resourceStackMatches = 0;
Term? backingActual = null;
Term backingExpected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "backing.etf")));
int currentCase = -1;
string currentFunction = string.Empty;
var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        if (requestedDepth is int depth)
        {
            await runtime.Modules.Call(
                context,
                "erlang",
                "system_flag",
                Term.A("backtrace_depth"),
                Term.I(depth)
            );
        }
        for (int index = 0; index < rows.Length; index++)
        {
            currentCase = index;
            if (index % 250 == 0)
            {
                Console.WriteLine($"{args[2]}: case {index}/{rows.Length}");
            }
            var row = (TupleTerm)rows[index];
            string function = ((Atom)row.Items[0]).Name;
            currentFunction = function;
            Term[] arguments = Cons.Items(row.Items[1]).ToArray();
            Term actual = await runtime.Modules.Call(
                context,
                StringCompatibilityNames.Observer,
                "run",
                Term.A(function),
                Cons.From(arguments)
            );
            observations.Add(Term.Tuple(row.Items[0], row.Items[1], actual));
            var expected = (TupleTerm)row.Items[2];
            var outcome = (TupleTerm)actual;
            bool matches = expected.Items[0].Equals(outcome.Items[0]) && (expected.Items[0].Equals(Term.A(StringCompatibilityNames.Returned))
                ? expected.Items[1].Equals(outcome.Items[1])
                : expected.Items[1].Equals(outcome.Items[1]) && expected.Items[2].Equals(outcome.Items[2]));
            if (matches)
            {
                if (expected.Items.Count == 2)
                {
                    values++;
                }
                else
                {
                    errors++;
                    if (StringResourceComparison.StacksEqual(expected, outcome))
                    {
                        stackMatches++;
                        if (expected.Items[3].Equals(outcome.Items[3]))
                        {
                            rawStackMatches++;
                        }
                        else
                        {
                            resourceStackMatches++;
                        }
                        if (!StringResourceComparison.ControlsRejectAlterations(expected, outcome))
                        {
                            throw new InvalidOperationException(StringCompatibilityDiagnostics.ResourceControls);
                        }
                    }
                    else
                    {
                        stackFailures.Add(new { Index = index, Function = function, Arguments = row.Items[1].ToString(), Expected = expected.Items[3].ToString(), Actual = outcome.Items[3].ToString() });
                    }
                }
            }
            else
            {
                failures.Add(new { Index = index, Function = function, Arguments = row.Items[1].ToString(), Expected = expected.ToString(), Actual = actual.ToString() });
            }
        }
        backingActual = await runtime.Modules.Call(context, StringCompatibilityNames.Observer, StringCompatibilityNames.Backing);
        done.TrySetResult();
    }
    catch (Exception failure)
    {
        Console.Error.WriteLine(StringCompatibilityDiagnostics.ExecutionCase(currentCase, currentFunction));
        done.TrySetException(failure);
    }

    return Nil.Value;
});
await done.Task.WaitAsync(TimeSpan.FromMinutes(10));
await actor.Completion;
File.WriteAllBytes(Path.Combine(directory, args[2] + "-actual.etf"), ExternalTermFormat.Encode(Cons.From(observations)));
File.WriteAllBytes(Path.Combine(directory, args[2] + "-backing-actual.etf"), ExternalTermFormat.Encode(backingActual!));
var report = new
{
    Baseline = "OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5",
    Backend = args[2],
    BacktraceDepth = runtime.BacktraceDepth,
    Selection = runtime.LibrarySelection.Modules.Select(module => new { module.Module, module.Backend, module.InputSha256, module.ManagedAssemblies, module.Dependencies }),
    ExportCount = expectedExports.Count,
    ExercisedExports = rows.Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, Cons.Items(row.Items[1]).Count())).Distinct().Count(),
    ApiCoverage = rows.Cast<TupleTerm>()
        .GroupBy(row => (Function: ((Atom)row.Items[0]).Name, Arity: Cons.Items(row.Items[1]).Count()))
        .OrderBy(group => group.Key.Function, StringComparer.Ordinal)
        .ThenBy(group => group.Key.Arity)
        .Select(group => new
        {
            group.Key.Function,
            group.Key.Arity,
            Cases = group.Count(),
            Values = group.Count(row => ((TupleTerm)row.Items[2]).Items.Count == 2),
            Errors = group.Count(row => ((TupleTerm)row.Items[2]).Items.Count > 2)
        }),
    Cases = rows.Length,
    ValuesMatched = values,
    ErrorReasonsMatched = errors,
    FullStacksMatched = stackMatches,
    RawStacksMatched = rawStackMatches,
    BoundResourceStacksMatched = resourceStackMatches,
    Failed = failures.Count,
    Failures = failures,
    StackDifferences = stackFailures.Count,
    StackFailures = stackFailures,
    ValuesAndReasonsPass = failures.Count == 0,
    CompleteOutcomesPass = failures.Count == 0 && stackFailures.Count == 0 && backingExpected.Equals(backingActual),
    BackingMatched = backingExpected.Equals(backingActual),
    AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(StringModule).Assembly.Location))),
    CorpusSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "original.etf"))))
};
string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(directory, args[2] + "-report.json"), json);
Console.WriteLine($"{args[2]}: {rows.Length} cases; values {values}; error reasons {errors}; exact stacks {stackMatches}; value/reason failures {failures.Count}; stack differences {stackFailures.Count}");

return failures.Count == 0 && stackFailures.Count == 0 && backingExpected.Equals(backingActual) ? 0 : 1;
