namespace Erlang;

internal sealed partial class ProcessTermHeap506
{
    internal ModuleLiteralDomain800? LiteralDomain800 { get; private set; }

    internal static ProcessTermHeap506 CreateModuleLiteral800(ProcessRuntime runtime, ModuleLiteralDomain800 domain)
        => new(runtime) { LiteralDomain800 = domain };

    internal HashSet<ProcessTermSequence506> CaptureModuleLiteralBlocks800(ModuleLiteralDomain800 domain)
    {
        if (!Monitor.IsEntered(Gate) || !ReferenceEquals(domain, LiteralDomain800) || domain.IsSealed800)
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);

        return new(blocks.Values, ReferenceEqualityComparer.Instance);
    }

    internal Term AdoptModuleLiteral800(
        Term source,
        ProcessTermConstruction514 construction,
        PreparedMapGraph517 maps,
        PreparedMapGraphUse517 admitted,
        Dictionary<Term, Term> copies
    )
    {
        if (!Monitor.IsEntered(Gate) || LiteralDomain800 is not { IsSealed800: false })
            throw new InvalidOperationException(ModuleLiteral800Names.Invalid);

        return AdoptGuestCore800(
            source,
            construction,
            maps,
            admitted,
            copies
        );
    }
}
