namespace Erlang;

public sealed partial class ProcessRuntime
{
    internal AtomNameTable AtomNames { get; }

    public int AtomCount => AtomNames.Count;

    public int AtomLimit => AtomNames.Capacity;

    /// <summary>Associates host-side term construction and decoding with this VM.</summary>
    public IDisposable EnterAtomScope()
    {
        ThrowIfHalted();
        ObjectDisposedException.ThrowIf(disposed, this);

        return new AtomNameScope(AtomNames);
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private void AtomTableExhausted()
    {
        RequestHaltCore(
            new RuntimeHaltRequest(
                RuntimeHaltKind.Numeric,
                AtomRuntimeParameters.ExhaustedExitStatus,
                null,
                new RuntimeHaltOptions(false)
            ),
            true
        );
        throw new ProcessHaltTransfer(this);
    }
}
