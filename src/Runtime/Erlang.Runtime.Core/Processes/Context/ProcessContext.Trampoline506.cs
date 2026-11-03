namespace Erlang;

public sealed partial class ProcessContext
{
    internal OwnedTermInvocation506? EnterOwnedTrampoline506(IReadOnlyList<Term> arguments)
        => OwnedExecutionEnabled683 ? new(
            this,
            arguments,
            false,
            true
        ) : null;
}
