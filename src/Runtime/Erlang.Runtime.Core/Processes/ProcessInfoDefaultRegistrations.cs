namespace Erlang;

// The default debugging query supports the existing genuine logical subset.
// Native heap/stack/GC fields remain absent; process_info/2 support is unchanged.
internal static class ProcessInfoDefaultRegistrations
{
    internal static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        ProcessInfoDefaultNames.Arity,
        async (context, arguments) =>
        {
            var projection = await context.Runtime.QueryDefaultLogicalProcessInfo(context, arguments[0]);
            context.Cancellation.ThrowIfCancellationRequested();

            return projection.Exists ? Cons.From(projection.AvailableFields) : ProcessInfoAtoms.Undefined;
        }
    );
}
