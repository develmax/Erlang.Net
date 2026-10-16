namespace Erlang.Otp;

internal static class SysTerminationAtoms
{
    public static readonly Atom Module = Term.A(SysTerminationNames.Module);
    public static readonly Atom Terminate = Term.A(SysTerminationNames.Terminate);
    public static readonly Atom Global = Term.A(SysTerminationNames.Global);
    public static readonly Atom Via = Term.A(SysTerminationNames.Via);
    public static readonly Atom System = Term.A(SysTerminationNames.System);
    public static readonly Atom Alias = Term.A(SysTerminationNames.Alias);
    public static readonly Atom Demonitor = Term.A(SysTerminationNames.Demonitor);
    public static readonly Atom NoConnect = Term.A(SysTerminationNames.NoConnect);
    public static readonly Atom NodeDown = Term.A(SysTerminationNames.NodeDown);
    public static readonly Atom Disconnected = Term.A(SysTerminationNames.Disconnected);
    public static readonly Atom CallingSelf = Term.A(SysTerminationNames.CallingSelf);
    public static readonly Term AliasOptions = Term.List(Term.Tuple(Alias, Demonitor));
    public static readonly Term SendOptions = Term.List(NoConnect);
}
