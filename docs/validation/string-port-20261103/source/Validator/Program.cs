using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

if (args.Length != 3)
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
Term[] rows = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "original.etf")))).ToArray();
Term[] declared = Cons.Items(ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf")))).ToArray();
var expectedExports = declared.Cast<TupleTerm>().Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value)).ToHashSet();
if (!expectedExports.SetEquals(StringModule.Exports))
{
    throw new InvalidOperationException(StringCompatibilityDiagnostics.ExportMismatch);
}

await using var runtime = new ProcessRuntime();
Console.WriteLine($"{args[2]}: preparing explicit original Unicode dependencies");
string foundation = Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp");
await runtime.InitializeModulesAsync(registry =>
{
    RuntimeNativePrimitiveProviders.RegisterCatalog(runtime, registry);
    string[] names = managed ? ["unicode", "unicode_util", "erts_internal"] : ["unicode", "unicode_util", "erts_internal", "string"];
    var dependencies = new BeamDependencySelection(Directory.EnumerateFiles(Path.Combine(repository, "src/Otp/Original"), "*.beam", SearchOption.AllDirectories)
        .Select(path => Path.GetFileNameWithoutExtension(path)).Append("erlang").Append(StringCompatibilityNames.Observer));
    var prepared = names.Select(name => registry.PrepareBeam(
        File.ReadAllBytes(Path.Combine(
            name == "erts_internal" ? Path.Combine(repository, "src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam/Otp/ebin") : foundation,
            name + ".beam"
        )),
        dependencies
    )).Append(registry.PrepareBeam(File.ReadAllBytes(Path.Combine(directory, "string_port_cases.beam")), dependencies)).ToArray();
    if (registry.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
    {
        throw new InvalidOperationException(StringCompatibilityDiagnostics.PublicationRefused);
    }
    if (managed)
    {
        StringModule.Register(registry);
    }

});
Console.WriteLine($"{args[2]}: executing {rows.Length} cases");

var observations = new List<Term>();
var failures = new List<object>();
var stackFailures = new List<object>();
int values = 0;
int errors = 0;
int stackMatches = 0;
Term? backingActual = null;
Term backingExpected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "backing.etf")));
var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
ProcessHandle actor = runtime.Spawn(async context =>
{
    try
    {
        for (int index = 0; index < rows.Length; index++)
        {
            if (index % 250 == 0)
            {
                Console.WriteLine($"{args[2]}: case {index}/{rows.Length}");
            }
            var row = (TupleTerm)rows[index];
            string function = ((Atom)row.Items[0]).Name;
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
                    if (expected.Items[3].Equals(outcome.Items[3]))
                    {
                        stackMatches++;
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
        done.TrySetException(failure);
    }

    return Nil.Value;
});
await done.Task.WaitAsync(TimeSpan.FromMinutes(5));
await actor.Completion;
File.WriteAllBytes(Path.Combine(directory, args[2] + "-actual.etf"), ExternalTermFormat.Encode(Cons.From(observations)));
File.WriteAllBytes(Path.Combine(directory, args[2] + "-backing-actual.etf"), ExternalTermFormat.Encode(backingActual!));
var report = new
{
    Baseline = "OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5",
    Backend = args[2],
    ExportCount = expectedExports.Count,
    ExercisedExports = rows.Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, Cons.Items(row.Items[1]).Count())).Distinct().Count(),
    Cases = rows.Length,
    ValuesMatched = values,
    ErrorReasonsMatched = errors,
    FullStacksMatched = stackMatches,
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
