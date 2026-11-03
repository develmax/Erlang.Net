namespace Erlang;

public sealed partial class ProcessContext
{
    // Set only by the INTERNAL genuine spawn/source-root dispatcher. Public
    // Modules.Call and public FunctionTerm.Invoke do not enter this boundary.
    internal bool VmSourceRoot641 { get; set; }
}
