namespace Erlang;

internal sealed partial class ProcessTermConstruction514
{
    private readonly List<Term>? forkAdds517;
    private bool forkCommitted517;
    internal ProcessTermFrame506 Frame517 => frame;

    private ProcessTermConstruction514(ProcessTermFrame506 frame, ProcessTermConstruction514 parent)
    {
        this.frame = frame;
        admitted = parent.admitted;
        forkAdds517 = new();
    }

    internal ProcessTermConstruction514 Fork517(ProcessTermFrame506 target) => new(target, this);

    internal void CommitFork517() => forkCommitted517 = true;

    internal void RollbackFork517()
    {
        if (!forkCommitted517 && forkAdds517 is not null)
            foreach (Term value in forkAdds517)
                admitted.Remove(value);
    }
}
