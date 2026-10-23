namespace Erlang.Otp;

internal static class ErpcAtoms
{
    internal static readonly Atom Module = Term.A(ErpcNames.Module);
    internal static readonly Atom Execute = Term.A(ErpcNames.Execute);
    internal static readonly Atom Return = Term.A(ErpcNames.Return);
    internal static readonly Atom Throw = Term.A(ErpcNames.Throw);
    internal static readonly Atom Exit = Term.A(ErpcNames.Exit);
    internal static readonly Atom Error = Term.A(ErpcNames.Error);
    internal static readonly Atom Exception = Term.A(ErpcNames.Exception);
    internal static readonly Atom Signal = Term.A(ErpcNames.Signal);
    internal static readonly Atom BadRpc = Term.A(ErpcNames.BadRpc);
    internal static readonly Atom ExitTag = Term.A(ErpcNames.ExitTag);
    internal static readonly Atom Timeout = Term.A(ErpcNames.Timeout);
    internal static readonly Atom TimeoutKey = Term.A(ErpcNames.Timeout);
    internal static readonly Atom AlwaysSpawn = Term.A(ErpcNames.AlwaysSpawn);
    internal static readonly Atom Infinity = Term.A(ErpcNames.Infinity);
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
    internal static readonly Atom SystemLimit = Term.A(ErlangErrorReasons.SystemLimit);
    internal static readonly Atom NoConnection = Term.A(ErpcNames.NoConnection);
    internal static readonly Atom NodeDown = Term.A(ErpcNames.NodeDown);
    internal static readonly Atom NotSupported = Term.A(ErpcNames.NotSupported);
    internal static readonly Atom True = Term.A(ErlangBooleanAtoms.True);
    internal static readonly Atom False = Term.A(ErlangBooleanAtoms.False);
}
