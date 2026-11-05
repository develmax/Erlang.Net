using System.Text.Json;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class OrdsetsTermContractChecks
{
    internal static async Task Run(int shard, bool original = false)
    {
        if (shard < 0 || shard >= OrdsetsTermContractNames.ShardCount)
            throw new ArgumentOutOfRangeException(nameof(shard));
        int firstStart = shard * OrdsetsTermContractNames.TermsPerShard;
        int firstEnd = Math.Min(firstStart + OrdsetsTermContractNames.TermsPerShard, OrdsetsTermContractNames.TermCount);
        using Stream resource = typeof(OrdsetsTermContractChecks).Assembly.GetManifestResourceStream(OrdsetsTermContractNames.Resource)!;
        using var bytes = new MemoryStream();
        await resource.CopyToAsync(bytes);
        await using var runtime = original
            ? await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([
                RuntimeModuleSelection.Beam(OrdsetsTermContractNames.Module, ReadOriginal(BeamOrdsetsCoreHostNames.Ordsets), [BeamOrdsetsCoreHostNames.Lists]),
                RuntimeModuleSelection.Beam(BeamOrdsetsCoreHostNames.Lists, ReadOriginal(BeamOrdsetsCoreHostNames.Lists))
            ]))
            : await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(OrdsetsTermContractNames.Module)]));
        var backend = runtime.LibrarySelection.Modules.Single(module => module.Module == OrdsetsTermContractNames.Module);
        if (backend.Backend != (original ? RuntimeModuleBackend.Beam : RuntimeModuleBackend.Managed)
            || original && runtime.Modules.Exports.Where(export => export.Module == OrdsetsTermContractNames.Module)
                .Any(export => runtime.Modules.SourceOwner(export.Module, export.Function, export.Arity) is not PreparedBeamImage))
            throw new InvalidDataException(OrdsetsTermContractNames.Failure);
        Term fixture = runtime.ImportExternalTerm(bytes.ToArray()).Term;
        using Stream observer = typeof(OrdsetsTermContractChecks).Assembly.GetManifestResourceStream(OrdsetsTermContractNames.ObserverImage)!;
        using var imageBytes = new MemoryStream();
        await observer.CopyToAsync(imageBytes);
        PreparedBeamImage image = runtime.Modules.PrepareBeam(
            imageBytes.ToArray(),
            new BeamDependencySelection([
            OrdsetsTermContractNames.Observer, OrdsetsTermContractNames.Module, "erlang", "lists", "maps", "binary"
        ])
        );
        if (runtime.Modules.PublishPreparedBeam([image]).Status != BeamPublicationStatus.Published)
            throw new InvalidDataException(OrdsetsTermContractNames.Failure);
        int termCount = 0;
        var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                Term constructed = await runtime.Modules.Call(
                    context,
                    OrdsetsTermContractNames.Observer,
                    OrdsetsTermContractNames.Pool
                );
                Term[] terms = Cons.Items(constructed).ToArray();
                termCount = terms.Length;
                using Stream probes = typeof(OrdsetsTermContractChecks).Assembly.GetManifestResourceStream(OrdsetsTermContractNames.ProbeResource)!;
                using var probeBytes = new MemoryStream();
                await probes.CopyToAsync(probeBytes);
                Term observedProbes = await runtime.Modules.Call(
                    context,
                    OrdsetsTermContractNames.Observer,
                    OrdsetsTermContractNames.ProbeEntry
                );
                if (Cons.Items(observedProbes).Count() != OrdsetsTermContractNames.ProbeCount
                    || !ExternalTermFormat.Encode(observedProbes).AsSpan().SequenceEqual(probeBytes.ToArray()))
                    throw new InvalidDataException(OrdsetsTermContractNames.Failure);
                int count = 0;
                foreach (var row in Cons.Items(fixture).Cast<TupleTerm>())
                {
                    int first = (int)((Integer)row.Items[0]).Value;
                    if (first < firstStart || first >= firstEnd)
                        continue;
                    int second = (int)((Integer)row.Items[1]).Value;
                    int expectedOrder = (int)((Integer)row.Items[2]).Value;
                    bool expectedNumeric = ((Atom)row.Items[3]).Name == OrdsetsTermContractNames.True;
                    bool expectedExact = ((Atom)row.Items[4]).Name == OrdsetsTermContractNames.True;
                    Term left = terms[first];
                    Term right = terms[second];
                    int order = Math.Sign(TermOrder.Compare(left, right));
                    bool numeric = IsTrue(await runtime.Modules.Call(
                        context,
                        OrdsetsTermContractNames.Module,
                        OrdsetsTermContractNames.Equality,
                        Term.List(left),
                        Term.List(right)
                    ));
                    bool membership = IsTrue(await runtime.Modules.Call(
                        context,
                        OrdsetsTermContractNames.Module,
                        OrdsetsTermContractNames.Membership,
                        left,
                        Term.List(right)
                    ));
                    bool isSet = IsTrue(await runtime.Modules.Call(
                        context,
                        OrdsetsTermContractNames.Module,
                        OrdsetsTermContractNames.IsSet,
                        Term.List(left, right)
                    ));
                    bool exact = left.Equals(right);
                    if (order != expectedOrder || numeric != expectedNumeric || exact != expectedExact
                        || membership != (expectedOrder == 0) || isSet != (expectedOrder < 0))
                        throw new InvalidDataException(OrdsetsTermContractNames.Failure + JsonSerializer.Serialize(new
                        {
                            first,
                            second,
                            order,
                            expectedOrder,
                            numeric,
                            expectedNumeric,
                            exact,
                            expectedExact,
                            membership,
                            isSet,
                            Left = left.ToString(),
                            Right = right.ToString(),
                            Stdlib = typeof(MapOperations).Assembly.ManifestModule.ModuleVersionId
                        }));
                    count++;
                }
                completed.TrySetResult(count);
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        int comparisons = await completed.Task.WaitAsync(OrdsetsTermContractNames.Deadline);
        await actor.Completion.WaitAsync(OrdsetsTermContractNames.Deadline);
        if (termCount != OrdsetsTermContractNames.TermCount
            || comparisons != (firstEnd - firstStart) * OrdsetsTermContractNames.TermCount)
            throw new InvalidDataException(OrdsetsTermContractNames.Failure);
        Directory.CreateDirectory(Path.GetDirectoryName(OrdsetsTermContractNames.Output)!);
        string output = Path.ChangeExtension(OrdsetsTermContractNames.Output, backend.Backend + "." + shard + ".json");
        await File.WriteAllTextAsync(
            output,
            JsonSerializer.Serialize(
                new
                {
                    Backend = backend.Backend.ToString(),
                    Shard = shard,
                    FirstStart = firstStart,
                    FirstEnd = firstEnd,
                    Terms = termCount,
                    Pairs = comparisons,
                    GuestApiProbes = OrdsetsTermContractNames.ProbeCount,
                    WholeMatrixMatches = true,
                    TermsAssembly = typeof(Term).Assembly.ManifestModule.ModuleVersionId,
                    RuntimeAssembly = typeof(ProcessRuntime).Assembly.ManifestModule.ModuleVersionId
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }

    private static byte[] ReadOriginal(string module)
    {
        string name = module + BeamOrdsetsCoreHostNames.BeamExtension;
        using Stream input = typeof(OrdsetsTermContractChecks).Assembly.GetManifestResourceStream(BeamOrdsetsCoreHostNames.ResourcePrefix + name)!;
        using var bytes = new MemoryStream();
        input.CopyTo(bytes);
        byte[] result = bytes.ToArray();
        if (Convert.ToHexString(SHA256.HashData(result)) != BeamOrdsetsCoreHostNames.Digests[name])
            throw new InvalidDataException(OrdsetsTermContractNames.Failure);

        return result;
    }

    private static bool IsTrue(Term value) => value is Atom { Name: OrdsetsTermContractNames.True };

}
