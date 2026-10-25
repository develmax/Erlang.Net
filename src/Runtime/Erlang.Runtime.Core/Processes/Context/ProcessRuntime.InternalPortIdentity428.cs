namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal void RequireInternalPortIdentity428(Term identity)
    {
        if (identity is PortTerm port && port.LocalIdentity is not null && !ReferenceEquals(port.LocalIdentity, LocalIdentity))
            throw new InvalidOperationException(InternalPortLoaderNames428.Foreign);
    }
}
