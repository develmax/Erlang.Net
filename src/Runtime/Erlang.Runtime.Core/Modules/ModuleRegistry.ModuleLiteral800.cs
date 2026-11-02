namespace Erlang;

public sealed partial class ModuleRegistry
{
    private readonly HashSet<ModuleLiteralDomain800> moduleLiteralDomains800 = new(ReferenceEqualityComparer.Instance);

    internal void OwnModuleLiteral800(ModuleLiteralDomain800 domain)
    {
        // Existing callers preparing images from an Apply/configure callback
        // need an outer preparation phase before this feature can be enabled.
        // Never perform new native graph construction under retained M/R.
        if (Monitor.IsEntered(gate) || CodeModification.IsOwned)
            throw new ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure.WriterOrder);
        domain.Runtime800.RequireModuleLiteralPreparation800();
        lock (gate)
        {
            ThrowIfSourceClosing();
            ThrowIfNifClosing();
            if (!ReferenceEquals(owningRuntime, domain.Runtime800))
                throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            moduleLiteralDomains800.Add(domain);
        }
    }

    private Task[] CloseModuleLiteralOwners800()
        => moduleLiteralDomains800.Select(domain => domain.CloseCodeOwner800()).ToArray();

    private static Task CloseModuleLiteralCodeOwner800(ModuleCodeImage image, Task nativeRetirement)
    {
        // Existing per-image NIF joins must not wait for ordinary processes or
        // queued literal holders. VM shutdown separately joins every tracked
        // arena. Purge/on_load rejection closes only new code-owner admission.
        _ = image.LiteralDomain819?.CloseCodeOwner800();

        return nativeRetirement;
    }

    internal Task DisposePreparedModuleLiteral800(PreparedBeamImage prepared)
    {
        if (TryReleasePreparedProducer807(prepared)) return Task.CompletedTask;
        lock (gate)
        {
            if (!ReferenceEquals(prepared.Owner, this)) throw new InvalidOperationException(ModuleLiteral800Names.Invalid);
            if (prepared.Consumed) return Task.CompletedTask;
            prepared.Disposed800 = true;

            return prepared.LiteralDomain800?.CloseCodeOwner800() ?? Task.CompletedTask;
        }
    }

    internal void ModuleLiteralRetired800(ModuleLiteralDomain800 domain)
    {
        lock (gate) moduleLiteralDomains800.Remove(domain);
    }
}
