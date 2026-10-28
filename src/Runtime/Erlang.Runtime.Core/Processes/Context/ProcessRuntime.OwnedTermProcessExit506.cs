namespace Erlang;

public sealed partial class ProcessRuntime
{
    private Task OwnedTermProcessRetirement506(ProcessContext context, Task preceding)
    {
        if (!OwnedTermsPilot506)
            return preceding;

        // Terminate has already removed this process under the runtime gate.
        // Close dictionary admission, retaining every active invocation/borrow.
        // Return the physical wait; logical cleanup and DOWN do not await it.
        context.BeginOwnedTermRetirement506();

        return Task.WhenAll(preceding, context.OwnedTermRetirement506);
    }
}
