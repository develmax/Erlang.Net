namespace Erlang;

internal readonly struct SourceEvaluationTurn511 : IDisposable
{
    private readonly OwnedTermInvocation506? invocation;
    private readonly bool previousBody;

    internal SourceEvaluationTurn511(OwnedTermInvocation506 invocation, bool functionBody)
    {
        this.invocation = invocation;
        previousBody = invocation.SourceFunctionBody511;
        if (invocation.SourceEvaluationDepth511 == 0)
            invocation.SourceFunctionBody511 = functionBody;
        invocation.SourceEvaluationDepth511 = checked(invocation.SourceEvaluationDepth511 + 1);
    }

    public void Dispose()
    {
        if (invocation is not null)
        {
            invocation.SourceEvaluationDepth511--;
            if (invocation.SourceEvaluationDepth511 == 0)
                invocation.SourceFunctionBody511 = previousBody;
        }
    }
}
