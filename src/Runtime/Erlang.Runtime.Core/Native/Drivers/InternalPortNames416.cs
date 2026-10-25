namespace Erlang;

internal static class InternalPortNames416
{
    internal const string Module = "erts_internal";
    internal const string Foreign = "Internal port entry requires its actual runtime actor and registry";
    internal const string Conflict = "Internal port native entry conflicts with an existing binding";
    internal static readonly (string Function, int Arity)[] Signatures =
    [
        (PortNames.Open, 2),
        (PortNames.Command, 3),
        (PortNames.Connect, 2),
        (PortNames.Close, 1),
        (PortNames.Info, 1),
        (PortNames.Info, 2)
    ];
}
