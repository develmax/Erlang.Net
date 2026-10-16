namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal LocalFunctionIdentity? ResolveSourceLocalIdentity(LocalFunctionIdentity identity, int arity)
    {
        var source = SourceLocalFunction(identity);
        var resolved = source?.Resolver.ResolveLocalIdentity(identity, arity);

        return resolved is null ? null : identity with { Name = resolved.Name };
    }

    internal FunctionTerm? ResolveSourceLocalFunction(
        LocalFunctionIdentity identity,
        int arity,
        IReadOnlyList<Term> environment,
        LocalFunctionInvocation invocation
    )
    {
        lock (gate)
            if (!invocation.IsCurrent)
                throw new LocalFunctionPublicationChangedException(invocation);
        var source = SourceLocalFunction(identity);

        // No guest code executes here. Invocation retains source image admission,
        // pending-purge waits, execution leases and publication rechecks.
        if (source is null)
            return null;

        return source.Value.Resolver.ResolveLocalFunction(
            identity,
            arity,
            environment,
            source.Value.Image,
            invocation
        );
    }

    private (ISourceLocalFunctionResolver Resolver, ModuleCodeImage Image)? SourceLocalFunction(LocalFunctionIdentity identity)
    {
        lock (gate)
        {
            string module = identity.Module.Name;
            if (codeDescriptors.TryGetValue(module, out var descriptor) &&
                descriptor.Fingerprint.Equals(identity.Unique) &&
                descriptor.SourceOwner is ISourceLocalFunctionResolver current &&
                sourceOwners.Any(entry => entry.Key.Module == module && ReferenceEquals(entry.Value, current)))
                return (current, codeImages[module]);
            if (oldCode.TryGetValue(module, out var old) &&
                old.Descriptor is { } oldDescriptor &&
                oldDescriptor.Fingerprint.Equals(identity.Unique) &&
                oldDescriptor.SourceOwner is ISourceLocalFunctionResolver previous &&
                old.SourceOwners.Any(owner => ReferenceEquals(owner, previous)))
                return (previous, old.Image);

            return null;
        }
    }
}
