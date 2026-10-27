namespace Erlang;

// A materialized layout contains managed immutable Terms only. In particular it
// never retains a pending graph, native binary storage, or NIF environment.
internal sealed partial class NativeMapMaterializedLayout505
{
    private readonly WeakReference<ProcessRuntime> owner;
    internal object Domain { get; }
    internal IReadOnlyList<KeyValuePair<Term, Term>> PhysicalEntries { get; }
    internal HamtViewNode? Root { get; }
    internal NifNativeMapHashRetention504? HashRetention { get; }

    internal NativeMapMaterializedLayout505(
        ProcessRuntime runtime,
        object domain,
        IReadOnlyList<KeyValuePair<Term, Term>> entries,
        HamtViewNode? root,
        NifNativeMapHashRetention504? retention
    )
    {
        owner = new(runtime);
        Domain = domain;
        PhysicalEntries = Array.AsReadOnly(entries.ToArray());
        Root = root;
        HashRetention = retention;
    }

    internal bool Matches(ProcessRuntime runtime, object? domain = null)
        => owner.TryGetTarget(out var original) && ReferenceEquals(original, runtime)
            && (domain is null || ReferenceEquals(Domain, domain));

    internal HamtIndex429 Index()
        => new(
            Root!,
            HashRetention!.Ownership,
            PhysicalEntries.Count,
            HashRetention
        );

    internal (bool Found, Term? Value) Lookup(Term key)
    {
        var runtime = RequireOwner();
        using var hashes = runtime.MapHashIdentityProvider(OtpMapHashLayoutProtocol.Append);
        bool found = HamtIndexLookup429.TryGet(
            Index(),
            key,
            hashes.Hash(key),
            out var value
        );

        return (found, value);
    }

    private ProcessRuntime RequireOwner()
    {
        if (!owner.TryGetTarget(out var runtime))
            throw new ObjectDisposedException(nameof(ProcessRuntime));
        runtime.RequireMapHashOpen();

        return runtime;
    }
}
