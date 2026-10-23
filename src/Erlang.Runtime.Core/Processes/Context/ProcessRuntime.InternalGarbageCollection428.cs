namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal Term CollectInternalGarbage428(ProcessContext context, Term argument)
    {
        RequireBeamfileActor418(context);
        bool major = argument switch
        {
            Atom { Name: InternalGarbageCollectionNames428.Minor } => false,
            Atom { Name: InternalGarbageCollectionNames428.Major } => true,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };
        // Same authoritative policy and real collector as the public process-GC
        // implementation. State advances only after GC.Collect succeeds.
        context.CollectManagedGarbage(major, CollectManagedHeap);
        RequireBeamfileActor418(context);

        return CoreModules.Boolean(true);
    }
}
