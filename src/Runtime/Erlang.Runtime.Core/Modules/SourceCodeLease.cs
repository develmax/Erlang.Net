namespace Erlang;

public sealed class SourceCodeLease : IDisposable
{
    private ProcessContext? context;

    internal SourceCodeLease(ProcessContext context, ModuleCodeImage image, ModuleLiteralLease800? literals800 = null)
    {
        this.context = context;
        Image = image;
        LiteralLease800 = literals800;
    }

    public ModuleCodeImage Image { get; }
    internal ModuleLiteralLease800? LiteralLease800 { get; }

    public IDisposable Attach(object owner) => (context ?? throw new ObjectDisposedException(nameof(SourceCodeLease))).AttachSourceCode(Image, owner, LiteralLease800);

    public void Dispose()
    {
        ProcessContext? owner = Interlocked.Exchange(ref context, null);
        if (owner is null) return;
        try
        {
            owner.ReleaseSourceCode(Image);
        }
        finally
        {
            LiteralLease800?.Dispose();
        }
    }
}
