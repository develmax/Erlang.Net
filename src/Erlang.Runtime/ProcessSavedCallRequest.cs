namespace Erlang;

internal sealed class ProcessSavedCallRequest(
    ProcessContext requester,
    ReferenceTerm id,
    Term flag,
    Term value
)
{
    public ProcessContext Requester { get; } = requester;
    public ReferenceTerm Id { get; } = id;
    public Term Flag { get; } = flag;
    public Term Value { get; } = value;
}
