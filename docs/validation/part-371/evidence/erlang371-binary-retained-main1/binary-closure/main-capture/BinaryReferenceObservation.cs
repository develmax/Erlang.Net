using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class BinaryReferenceObservation
{
    internal static async Task Write(
        ProcessContext context,
        string directory,
        string group,
        Term values,
        Term profiles
    )
    {
        var observations = new List<object>();
        foreach (Term row in Cons.Items(values))
            await Read(context, group, BinaryReferenceObservationNames.Values, row, observations);
        foreach (Term profile in Cons.Items(profiles))
        {
            if (profile is not TupleTerm { Items.Count: 2 } pair)
                throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidObservation);
            string depth = pair.Items[0] is Atom atom ? atom.Name : pair.Items[0].ToString()!;
            foreach (Term row in Cons.Items(pair.Items[1]))
                await Read(context, group, depth, row, observations);
        }

        string runtime = typeof(ProcessRuntime).Assembly.Location;
        var result = new
        {
            Group = group,
            ReturnedSha256 = Digest(Path.Combine(directory, BinaryReferenceObservationNames.ReturnedFile)),
            StacksSha256 = Digest(Path.Combine(directory, BinaryReferenceObservationNames.StacksFile)),
            RuntimeAssemblySha256 = Digest(runtime),
            TermsAssemblySha256 = Digest(typeof(Term).Assembly.Location),
            Observations = observations,
            RegistryReadWhileRuntimeAlive = true
        };
        await System.IO.File.WriteAllTextAsync(
            Path.Combine(directory, BinaryReferenceObservationNames.File),
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
        );
    }

    private static async ValueTask Read(
        ProcessContext context,
        string group,
        string depth,
        Term row,
        List<object> observations
    )
    {
        if (row is not TupleTerm { Items.Count: 2 } named || named.Items[0] is not Atom name
            || named.Items[1] is not TupleTerm outcome
            || outcome.Items[^1] is not TupleTerm { Items.Count: 2 } effects)
            throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidObservation);
        foreach (Term provenance in Cons.Items(effects.Items[1]))
        {
            if (provenance is not TupleTerm source || source.Items[0] is not Atom role)
                throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidObservation);
            bool compiled = role.Name == BinaryReferenceObservationNames.Compiled;
            if (source.Items.Count != (compiled ? 6 : 2)
                || !compiled && role.Name != BinaryReferenceObservationNames.Ordinary
                || source.Items[compiled ? 2 : 1] is not ReferenceTerm reference)
                throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidObservation);
            object? runtimeIdentity = typeof(ProcessRuntime).GetProperty(
                BinaryReferenceObservationNames.LocalIdentity,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(context.Runtime);
            object? referenceIdentity = typeof(ReferenceTerm).GetProperty(
                BinaryReferenceObservationNames.LocalIdentity,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(reference);
            bool local = runtimeIdentity is not null && ReferenceEquals(runtimeIdentity, referenceIdentity);
            var acquire = typeof(RuntimeReferenceResources).GetMethod(
                BinaryReferenceObservationNames.Acquire,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!;
            object? lease = acquire.Invoke(context.Runtime.ReferenceResources, [reference]);
            string? tag = null;
            string? replay = null;
            try
            {
                if (lease is not null)
                {
                    object payload = lease.GetType().GetProperty(BinaryReferenceObservationNames.Value)!.GetValue(lease)!;
                    tag = (string)payload.GetType().GetProperty(
                        BinaryReferenceObservationNames.Tag,
                        BindingFlags.Instance | BindingFlags.NonPublic
                    )!.GetValue(payload)!;
                }
                if (compiled)
                {
                    Term replayed = await context.Runtime.Modules.Call(
                        context,
                        BinaryReferenceObservationNames.BinaryModule,
                        BinaryReferenceObservationNames.Match,
                        source.Items[4],
                        Term.Tuple(source.Items[1], reference)
                    );
                    replay = Convert.ToHexString(ExternalTermFormat.Encode(replayed));
                    if (!replayed.Equals(source.Items[5]))
                        throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidRegistry);
                }
            }
            finally
            {
                (lease as IDisposable)?.Dispose();
            }
            if (!local || compiled != (lease is not null)
                || compiled && reference is not ResourceReferenceTerm
                || !compiled && reference.GetType() != typeof(ReferenceTerm))
                throw new InvalidOperationException(BinaryReferenceObservationNames.InvalidRegistry);
            observations.Add(new
            {
                Group = group,
                Case = name.Name,
                Depth = depth,
                Role = role.Name,
                ReferenceEtfHex = Convert.ToHexString(ExternalTermFormat.Encode(reference)),
                ProvenanceEtfHex = Convert.ToHexString(ExternalTermFormat.Encode(provenance)),
                LocalIdentityBound = local,
                RegistryAcquired = lease is not null,
                ResourceKind = (reference as ResourceReferenceTerm)?.ResourceKind.ToString(),
                PayloadTag = tag,
                ReplayedMatchEtfHex = replay
            });
        }
    }

    private static string Digest(string path)
        => Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path)));
}
