namespace Erlang;

public sealed partial class ProcessRuntime
{
    // Called under the actual runtime gate by Terminate. A normal writer exit
    // leaves VM-global persistent values intact; initializer/VM exit joins owner.
    private Task PersistentProcessRetirement417(ProcessContext context, Task previous)
    {
        if (persistentTerms is null) return previous;
        Task? owner = context.Self.Equals(InitializationPid)
            ? persistentTerms.BeginRuntimeClose417()
            : persistentTerms.RequestedRuntimeRetirement417;
        if (owner is null || owner.IsCompletedSuccessfully) return previous;
        if (previous.IsCompletedSuccessfully) return owner;

        return Task.WhenAll(previous, owner);
    }
}
