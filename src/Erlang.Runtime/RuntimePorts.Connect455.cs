namespace Erlang;

internal sealed partial class RuntimePorts
{
    internal ValueTask<Term> ConnectOrdered455(ProcessContext caller, Term identity, Term destination)
    {
        if (runtime.DriverFrontendForPort422(identity) is { } native)
            return native.ConnectPort455(caller, identity, destination);

        return ValueTask.FromResult(Connect(identity, destination));
    }
}
