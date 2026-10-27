namespace Erlang;

internal sealed class SourceStatementRoots511 : IDisposable
{
    private readonly OwnedTermInvocation506 invocation;
    private ProcessTermFrame506 lexical;
    private readonly int watermark;

    internal SourceStatementRoots511(OwnedTermInvocation506 invocation)
    {
        this.invocation = invocation;
        watermark = invocation.Frame.SourceWatermark511;
        lexical = invocation.Frame.EnterSourceLexicalRoots511();
    }

    internal void PublishCompletedStatement(IReadOnlyList<Term> lexicalRoots)
        => lexical = invocation.Frame.PublishLexicalAndRetire511(
            lexical,
            watermark,
            invocation.ResultSlot507,
            lexicalRoots
        );

    public void Dispose() => lexical.Dispose();
}
