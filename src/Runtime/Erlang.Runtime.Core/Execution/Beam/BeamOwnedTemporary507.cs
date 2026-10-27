namespace Erlang;

internal sealed class BeamOwnedTemporary507 : IDisposable
{
    private readonly ProcessTermFrame506 frame;

    internal BeamOwnedTemporary507(ProcessContext context, Term graph)
    {
        frame = context.TermHeap506.Enter();
        try
        {
            frame.SetLocalGraph507(frame.Reserve(), graph);
        }
        catch
        {
            frame.Dispose();
            throw;
        }
    }

    public void Dispose() => frame.Dispose();
}
