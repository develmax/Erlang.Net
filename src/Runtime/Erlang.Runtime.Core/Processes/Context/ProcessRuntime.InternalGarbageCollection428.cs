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
        // Same actor-safe target collector as public process GC; no R/M walk.
        CollectAtProcessSafePoint(context, major);
        RequireBeamfileActor418(context);

        return CoreModules.Boolean(true);
    }
}
