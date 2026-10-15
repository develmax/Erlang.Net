namespace Erlang;

internal sealed class ProcessCodeRequest(
    ProcessContext requester,
    Term id,
    string module,
    ModuleCodeImage? image
)
{
    public ProcessContext Requester { get; } = requester;
    public Term Id { get; } = id;
    public string Module { get; } = module;
    public ModuleCodeImage? Image { get; } = image;
}
