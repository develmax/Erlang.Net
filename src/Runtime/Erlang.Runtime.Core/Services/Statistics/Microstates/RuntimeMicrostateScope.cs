namespace Erlang;

internal sealed class RuntimeMicrostateScope : IDisposable
{
    internal RuntimeMicrostateRecord? Record { get; private set; }
    internal RuntimeMicrostateScope? Previous { get; private set; }
    internal RuntimeMicrostate? PreviousState { get; set; }
    internal bool IsAttachment { get; }
    internal RuntimeMicrostate State { get; set; }

    internal RuntimeMicrostateScope(
        RuntimeMicrostateRecord record,
        RuntimeMicrostateScope? previous,
        RuntimeMicrostate? previousState,
        bool attachment
    )
    {
        Record = record;
        Previous = previous;
        PreviousState = previousState;
        IsAttachment = attachment;
    }

    public void Dispose()
    {
        if (Record is null) return;
        RuntimeMicrostateExecution.Leave(this);
        Record = null;
        Previous = null;
    }
}
