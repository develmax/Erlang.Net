namespace Erlang;

public sealed partial class ProcessContext
{
    internal OwnedTermInvocation506? EnterOwnedTrampoline506(IReadOnlyList<Term> arguments)
        => Runtime.OwnedTermsPilot506 ? new(
            this,
            arguments,
            false,
            true
        ) : null;
}
