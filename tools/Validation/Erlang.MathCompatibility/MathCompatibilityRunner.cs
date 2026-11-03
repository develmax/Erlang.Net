using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

internal static class MathCompatibilityRunner
{
    internal static async Task<int> Run(string[] args)
    {
        string directory = Path.GetFullPath(args[1]);
        MathOracleAdmission.Validate(directory);
        bool beam = args[0] == MathCompatibilityNames.Beam;
        byte[] mathImage = File.ReadAllBytes(Path.Combine(directory, MathCompatibilityNames.OriginalImage));
        RuntimeModuleSelection choice = beam
            ? RuntimeModuleSelection.Beam(MathCompatibilityNames.Module, mathImage)
            : RuntimeModuleSelection.Managed(MathCompatibilityNames.Module);
        var request = new RuntimeLibrarySelection([
            choice,
            RuntimeModuleSelection.Managed(MathCompatibilityNames.RuntimeModule),
            RuntimeModuleSelection.Beam(
                MathCompatibilityNames.Observer,
                File.ReadAllBytes(Path.Combine(directory, MathCompatibilityNames.ObserverImage)),
                [MathCompatibilityNames.RuntimeModule, MathCompatibilityNames.Module]
            )
        ]);
        await using var runtime = await ProcessRuntime.CreateAsync(request);
        byte[] bytes = File.ReadAllBytes(Path.Combine(directory, MathCompatibilityNames.OriginalRows));
        Term rows = runtime.ImportExternalTerm(bytes, maxBytes: bytes.Length).Term;
        Term declared = Import(MathCompatibilityNames.OriginalExports);
        var expectedExports = Items(declared).Cast<TupleTerm>()
            .Select(value => (((Atom)value.Items[0]).Name, (int)((Integer)value.Items[1]).Value)).ToHashSet();
        RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single(module => module.Module == MathCompatibilityNames.Module);
        if (expectedExports.Count != MathCompatibilityNames.ExpectedExports
            || !expectedExports.SetEquals(selected.Exports
                .Where(value => value.Name != MathCompatibilityNames.ModuleInformation)
                .Select(value => (value.Name, value.Arity))))
        {
            throw new InvalidDataException(MathCompatibilityDiagnostics.Exports);
        }
        if (selected.Backend != choice.Backend || beam && selected.InputSha256 != Convert.ToHexString(SHA256.HashData(mathImage))
            || selected.NativePrimitives.Count != MathCompatibilityNames.ExpectedPrimitives
            || !ReferenceEquals(selected.Image, runtime.Modules.CurrentCodeImage(MathCompatibilityNames.Module))
            || runtime.Modules.HasOldCode(MathCompatibilityNames.Module))
        {
            throw new InvalidDataException(MathCompatibilityDiagnostics.Ownership);
        }
        foreach (BeamFunctionSignature export in selected.Exports)
        {
            object? owner = runtime.Modules.SourceOwner(MathCompatibilityNames.Module, export.Name, export.Arity);
            if (beam ? owner is not PreparedBeamImage image || image.Name != MathCompatibilityNames.Module : owner is not null)
            {
                throw new InvalidDataException(MathCompatibilityDiagnostics.Ownership);
            }
        }
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        int failed = 0;
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                int count = 0;
                var failures = new List<object>();
                var failureCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (Term value in Items(rows))
                {
                    var row = (TupleTerm)value;
                    string function = ((Atom)row.Items[0]).Name;
                    Term[] inputs = Items(row.Items[1]).ToArray();
                    Term expected = row.Items[2];
                    Term actual;
                    try
                    {
                        actual = Term.Tuple(Term.A(MathCompatibilityNames.Ok),
                            await runtime.Modules.Call(
                                context,
                                MathCompatibilityNames.Module,
                                function,
                                inputs
                            ));
                    }
                    catch (ErlangException error)
                    {
                        actual = Term.Tuple(Term.A(MathCompatibilityNames.Error), error.Reason);
                    }
                    count++;
                    if (!Exact(expected, actual))
                    {
                        failed++;
                        failureCounts[function] = failureCounts.GetValueOrDefault(function) + 1;
                        if (failureCounts[function] <= MathCompatibilityNames.SamplesPerFunction)
                        {
                            failures.Add(new
                            {
                                Function = function,
                                Input = inputs.Select(input => input.ToString()).ToArray(),
                                Expected = Convert.ToHexString(ExternalTermFormat.Encode(expected)),
                                Actual = Convert.ToHexString(ExternalTermFormat.Encode(actual))
                            });
                        }
                    }
                }
                if (count != MathCompatibilityNames.ExpectedRows)
                {
                    throw new InvalidDataException(MathCompatibilityDiagnostics.Count);
                }
                Term values = await runtime.Modules.Call(context, MathCompatibilityNames.Observer, MathCompatibilityNames.Run);
                Term stacks = await runtime.Modules.Call(context, MathCompatibilityNames.Observer, MathCompatibilityNames.Stacks);
                bool wholeValues = Exact(Import(MathCompatibilityNames.OriginalValues), values);
                bool wholeStacks = Exact(Import(MathCompatibilityNames.OriginalStacks), stacks);
                if (!wholeValues || !wholeStacks)
                {
                    failed++;
                    failures.Add(new { Error = MathCompatibilityDiagnostics.StackMismatch, Values = wholeValues, Stacks = wholeStacks });
                }
                completion.TrySetResult(new
                {
                    Backend = selected.Backend.ToString(),
                    Count = count,
                    Failed = failed,
                    FailureCounts = failureCounts,
                    Samples = failures,
                    WholeValues = wholeValues,
                    WholeStacks = wholeStacks,
                    OracleMetadata = Import(MathCompatibilityNames.OriginalMetadata).ToString(),
                    OracleSha256 = Convert.ToHexString(SHA256.HashData(bytes)),
                    OracleReceiptSha256 = Convert.ToHexString(SHA256.HashData(
                        File.ReadAllBytes(Path.Combine(directory, MathCompatibilityNames.OracleIdentity)))),
                    MathImageSha256 = Convert.ToHexString(SHA256.HashData(mathImage)),
                    RuntimeSha256 = AssemblyHash(typeof(ProcessRuntime)),
                    TermsSha256 = AssemblyHash(typeof(Term)),
                    SerializationSha256 = AssemblyHash(typeof(ExternalTermFormat)),
                    Framework = RuntimeInformation.FrameworkDescription,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    OperatingSystem = RuntimeInformation.OSDescription,
                    CoreLibrarySha256 = AssemblyHash(typeof(Math)),
                    ValidatorSha256 = AssemblyHash(typeof(MathCompatibilityRunner)),
                    selected.ManagedAssemblies
                });
            }
            catch (Exception error)
            {
                completion.TrySetException(error);
            }

            return Nil.Value;
        });
        object report = await completion.Task.WaitAsync(MathCompatibilityNames.Deadline);
        await worker.Completion.WaitAsync(MathCompatibilityNames.CleanupDeadline);
        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(args[2], json);
        Console.WriteLine(json);

        return failed == 0 ? 0 : 1;

        Term Import(string name) => runtime.ImportExternalTerm(File.ReadAllBytes(Path.Combine(directory, name))).Term;

        static bool Exact(Term first, Term second)
            => ExternalTermFormat.Encode(first).AsSpan().SequenceEqual(ExternalTermFormat.Encode(second));

        static string AssemblyHash(Type type)
            => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(type.Assembly.Location)));

        static IEnumerable<Term> Items(Term term)
        {
            while (term is Cons cell)
            {
                yield return cell.Head;
                term = cell.Tail;
            }
            if (term is not Nil)
            {
                throw new InvalidDataException(MathCompatibilityDiagnostics.ImproperList);
            }
        }

    }
}
