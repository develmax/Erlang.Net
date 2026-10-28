namespace Erlang.Compiler;

internal sealed class SourceFunctionTransfer(SourceFunctionTailScope scope, SourceInvocationTarget target) : Exception, IOwnedControlTransfer506
{
    internal SourceFunctionTailScope Scope { get; } = scope;
    internal SourceInvocationTarget Target { get; } = target;

    Exception IOwnedControlTransfer506.TransferTerms506(Func<Term, Term> publish)
    {
        try
        {
            return new SourceFunctionTransfer(Scope, SourceContinuationPayload506.Target(Target, [], publish).Target);
        }
        catch
        {
            // Failed publication leaves no consumer for the admitted code lease.
            Target.Lease?.Dispose();
            throw;
        }
    }
}
