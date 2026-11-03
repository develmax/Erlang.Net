using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class OrdsetsCompatibilityChecks
{
    internal static async Task Run(bool original)
    {
        RuntimeModuleSelection selection = original
            ? RuntimeModuleSelection.Beam(
                OrdsetsCompatibilityNames.Module,
                BeamOrdsetsCoreHostChecks.Read(OrdsetsCompatibilityNames.Module + OrdsetsCompatibilityNames.ImageExtension),
                [OrdsetsCompatibilityNames.Lists]
            )
            : RuntimeModuleSelection.Managed(OrdsetsCompatibilityNames.Module);
        RuntimeModuleSelection lists = original
            ? RuntimeModuleSelection.Beam(
                OrdsetsCompatibilityNames.Lists,
                BeamOrdsetsCoreHostChecks.Read(OrdsetsCompatibilityNames.Lists + OrdsetsCompatibilityNames.ImageExtension)
            )
            : RuntimeModuleSelection.Managed(OrdsetsCompatibilityNames.Lists);
        await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
            lists, selection
        ]));
        RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single(
            module => module.Module == OrdsetsCompatibilityNames.Module
        );
        if (selected.Backend != (original ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed)
            || runtime.Modules.Exports.Count(export => export.Module == OrdsetsCompatibilityNames.Module
                && export.Function != OrdsetsCompatibilityNames.ModuleInfo)
                != OrdsetsCompatibilityNames.ExportCount)
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        if (original)
        {
            byte[] subject = BeamOrdsetsCoreHostChecks.Read(
                OrdsetsCompatibilityNames.Module + OrdsetsCompatibilityNames.ImageExtension
            );
            if (selected.InputSha256 != Convert.ToHexString(SHA256.HashData(subject))
                || selected.NativePrimitives.Count != 0
                || runtime.Modules.Exports.Where(export => export.Module == OrdsetsCompatibilityNames.Module)
                    .Any(export => runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity)
                        is not PreparedBeamImage))
                throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        }
        else if (selected.ManagedAssemblies.Count == 0)
        {
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        }
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            Read(OrdsetsCompatibilityNames.Image),
            new BeamDependencySelection([
                OrdsetsCompatibilityNames.Observer,
                OrdsetsCompatibilityNames.Module,
                OrdsetsCompatibilityNames.Lists,
                OrdsetsCompatibilityNames.Worker,
                OrdsetsCompatibilityNames.Erlang
            ])
        );
        PreparedBeamImage worker = runtime.Modules.PrepareBeam(
            Read(OrdsetsCompatibilityNames.Worker + OrdsetsCompatibilityNames.ImageExtension),
            new BeamDependencySelection([
                OrdsetsCompatibilityNames.Erlang,
                OrdsetsCompatibilityNames.Worker,
                OrdsetsCompatibilityNames.Observer
            ])
        );
        if (runtime.Modules.PublishPreparedBeam([image, worker]).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        // Inputs come from the admitted original observer's cases/0. Its run/0 and run/1
        // are independently required to produce the same whole deterministic ETF.
        var decoding = new EtfDecodeOptions
        {
            ExternalFunctionFactory = (identity, safe) => runtime.Functions.External(runtime, identity, safe)
        };
        Term[] inputs = Cons.Items(ExternalTermFormat.DecodeOne(Read(OrdsetsCompatibilityNames.Inputs), decoding).Term).ToArray();
        var completed = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                var encodedBatches = new List<byte[]>();
                foreach (Term[] batch in inputs.Chunk(OrdsetsCompatibilityNames.BatchSize))
                {
                    Term rows = await runtime.Modules.Call(
                        context,
                        OrdsetsCompatibilityNames.Worker,
                        OrdsetsCompatibilityNames.Entry,
                        Term.A(OrdsetsCompatibilityNames.Observer),
                        Term.A(OrdsetsCompatibilityNames.Entry),
                        Term.List(Cons.From(batch))
                    );
                    if (Cons.Items(rows).Count() != batch.Length)
                        throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
                    encodedBatches.Add(ExternalTermFormat.Encode(rows, new EtfEncodingOptions
                    {
                        FunctionOwner = runtime.InitializationPid,
                        Deterministic = true
                    }));
                }
                completed.TrySetResult(OrdsetsEncodedCorpus.Combine(encodedBatches, inputs.Length));
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        Task finished;
        try
        {
            finished = await Task.WhenAny(completed.Task, actor.Completion).WaitAsync(OrdsetsCompatibilityNames.Deadline);
        }
        catch (TimeoutException)
        {
            Directory.CreateDirectory(OrdsetsCompatibilityNames.FailureDirectory);
            var query = typeof(ProcessRuntime).GetMethod("ProcessInfo", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            Type itemType = query.GetParameters()[1].ParameterType.GetGenericArguments()[0];
            var parse = itemType.GetMethod("Parse")!;
            string[] names = ["status", "current_function", "reductions"];
            Array keys = Array.CreateInstance(itemType, names.Length);
            for (int index = 0; index < names.Length; index++)
                keys.SetValue(parse.Invoke(null, [Term.A(names[index])]), index);
            File.WriteAllLines(
                Path.Combine(OrdsetsCompatibilityNames.FailureDirectory, selected.Backend + ".timeout.txt"),
                runtime.ProcessSnapshot().Select(pid => pid + ": " + query.Invoke(runtime, [pid, keys, false]))
                    .Prepend("Actor completion: " + actor.Completion.Status)
            );
            throw;
        }
        if (ReferenceEquals(finished, actor.Completion))
        {
            await actor.Completion;
            if (!completed.Task.IsCompleted)
                throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        }
        byte[] encoded = await completed.Task;
        await actor.Completion.WaitAsync(OrdsetsCompatibilityNames.Deadline);
        Term actual = ExternalTermFormat.DecodeOne(encoded, decoding).Term;
        byte[] expected = Read(OrdsetsCompatibilityNames.Golden);
        var coverage = Cons.Items(actual).Cast<TupleTerm>()
            .GroupBy(row => (((Atom)row.Items[0]).Name, Cons.Items(row.Items[1]).Count()))
            .ToDictionary(group => group.Key, group => group.Count());
        var exports = runtime.Modules.Exports.Where(export => export.Module == OrdsetsCompatibilityNames.Module
            && export.Function != OrdsetsCompatibilityNames.ModuleInfo)
            .Select(export => (export.Function, export.Arity)).ToHashSet();
        if (!exports.SetEquals(coverage.Keys))
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);
        if (Cons.Items(actual).Count() != OrdsetsCompatibilityNames.ObservationCount
            || !encoded.AsSpan().SequenceEqual(expected))
        {
            Directory.CreateDirectory(OrdsetsCompatibilityNames.FailureDirectory);
            string backend = selected.Backend.ToString();
            File.WriteAllBytes(Path.Combine(OrdsetsCompatibilityNames.FailureDirectory, backend + ".actual.etf"), encoded);
            Term golden = runtime.ImportExternalTerm(expected).Term;
            Term[] actualRows = Cons.Items(actual).ToArray();
            Term[] expectedRows = Cons.Items(golden).ToArray();
            var differences = actualRows.Zip(expectedRows)
                .Select((pair, index) => (pair, index))
                .Where(row => !row.pair.First.Equals(row.pair.Second))
                .Take(10)
                .Select(row => row.index + ": " + row.pair.First + Environment.NewLine + "expected: " + row.pair.Second);
            File.WriteAllLines(Path.Combine(OrdsetsCompatibilityNames.FailureDirectory, backend + ".txt"), differences);

            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure + ": " + backend);
        }
        Directory.CreateDirectory(OrdsetsCompatibilityNames.ReceiptDirectory);
        await File.WriteAllTextAsync(
            Path.Combine(OrdsetsCompatibilityNames.ReceiptDirectory, selected.Backend + ".json"),
            JsonSerializer.Serialize(
                new
                {
                    Backend = selected.Backend.ToString(),
                    selected.InputSha256,
                    selected.ManagedAssemblies,
                    CorpusSha256 = Convert.ToHexString(SHA256.HashData(encoded)),
                    Observations = coverage.Values.Sum(),
                    ApiCount = coverage.Count,
                    Coverage = coverage.OrderBy(pair => pair.Key.Name, StringComparer.Ordinal)
                    .ThenBy(pair => pair.Key.Item2)
                    .Select(pair => new { Function = pair.Key.Name, Arity = pair.Key.Item2, Observations = pair.Value })
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static byte[] Read(string name)
    {
        using Stream stream = typeof(OrdsetsCompatibilityChecks).Assembly.GetManifestResourceStream(
            OrdsetsCompatibilityNames.Prefix + name
        )!;
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] bytes = output.ToArray();
        using Stream provenance = typeof(OrdsetsCompatibilityChecks).Assembly.GetManifestResourceStream(
            OrdsetsCompatibilityNames.Prefix + OrdsetsCompatibilityNames.Provenance
        )!;
        using JsonDocument receipt = JsonDocument.Parse(provenance);
        if (receipt.RootElement.GetProperty("PinnedOtpCommit").GetString() != OrdsetsCompatibilityNames.PinnedCommit
            || receipt.RootElement.GetProperty("ExitCode").GetInt32() != 0
            || receipt.RootElement.GetProperty("Files").GetProperty(name).GetString() != Convert.ToHexString(SHA256.HashData(bytes)))
            throw new InvalidDataException(OrdsetsCompatibilityNames.Failure);

        return bytes;
    }
}
