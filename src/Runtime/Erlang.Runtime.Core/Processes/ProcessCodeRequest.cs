namespace Erlang;

internal sealed class ProcessCodeRequest(
    ProcessContext requester,
    Term id,
    string module,
    ModuleCodeImage? image
)
{
    public ProcessContext Requester { get; } = requester;
    private readonly PortableControlTerm826 id826 = PortableControlTerm826.Capture(id);
    public Term Id => id826.Value;
    public string Module { get; } = module;
    public ModuleCodeImage? Image { get; } = image;
}
