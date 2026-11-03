namespace Erlang;

// Owns both the candidate register snapshot and metadata until interpreter
// admission. Rejected, cancelled and post-return selections must be disposed.
internal sealed class BeamReceiveSelection : IGuestMailboxSelection593
{
    private BeamRegisterState? registers;
    private IDisposable? metadata;
    private ProcessTermFrame506? mailboxCandidate593;

    public void AttachMailboxCandidate593(ProcessTermFrame506 candidate)
    {
        if (mailboxCandidate593 is not null)
            throw new InvalidOperationException(MailboxCustody593Names.DuplicateReceiveCandidate);
        mailboxCandidate593 = candidate;
    }

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
        ProcessTermFrame506? capturedCandidate = mailboxCandidate593;
        mailboxCandidate593 = null;
        MailboxReceiveCleanup593.Dispose(
            captured,
            capturedMetadata,
            capturedCandidate,
            null
        );
    }
}
