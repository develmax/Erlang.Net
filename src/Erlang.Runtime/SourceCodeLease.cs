namespace Erlang;

public sealed class SourceCodeLease : IDisposable
{
    private ProcessContext? context;

    internal SourceCodeLease(ProcessContext context, ModuleCodeImage image)
    {
        this.context = context;
        Image = image;
    }

    public ModuleCodeImage Image { get; }

    public IDisposable Attach(object owner) => (context ?? throw new ObjectDisposedException(nameof(SourceCodeLease))).AttachSourceCode(Image, owner);

    public void Dispose() => Interlocked.Exchange(ref context, null)?.ReleaseSourceCode(Image);
}
