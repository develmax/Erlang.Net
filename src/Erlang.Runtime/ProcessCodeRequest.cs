namespace Erlang;

internal sealed class ProcessCodeRequest(ProcessContext requester, Term id, string module)
{
    public ProcessContext Requester { get; } = requester;
    public Term Id { get; } = id;
    public string Module { get; } = module;
}
