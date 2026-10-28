namespace Erlang;

// Owns both the candidate register snapshot and metadata until interpreter
// admission. Rejected, cancelled and post-return selections must be disposed.
internal sealed class BeamReceiveSelection : IDisposable
{
    private BeamRegisterState? registers;
    private IDisposable? metadata;

    internal BeamReceiveSelection(
        int nextInstructionIndex,
        BeamRegisterState registers,
        IReadOnlyList<Term> clearMarkers
    )
    {
        NextInstructionIndex = nextInstructionIndex;
        ClearMarkers = clearMarkers;
        // The producer retains register ownership if publication throws.
        metadata = registers.Protect507(clearMarkers.ToArray());
        this.registers = registers;
    }

    internal int NextInstructionIndex { get; }
    internal IReadOnlyList<Term> ClearMarkers { get; }

    internal BeamRegisterState TakeRegisters507()
    {
        BeamRegisterState result = registers
            ?? throw new ObjectDisposedException(nameof(BeamReceiveSelection));
        registers = null;

        return result;
    }

    public void Dispose()
    {
        BeamRegisterState? captured = registers;
        registers = null;
        IDisposable? capturedMetadata = metadata;
        metadata = null;
        try
        {
            captured?.Dispose();
        }
        finally
        {
            capturedMetadata?.Dispose();
        }
    }
}
