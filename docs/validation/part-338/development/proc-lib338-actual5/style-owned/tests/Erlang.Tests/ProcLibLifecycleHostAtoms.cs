namespace Erlang.Tests;

internal static class ProcLibLifecycleHostAtoms
{
    public static readonly Atom Ok = Term.A(ProcLibLifecycleHostNames.Ok);
    public static readonly Atom True = Term.A(ProcLibLifecycleHostNames.True);
    public static readonly Atom False = Term.A(ProcLibLifecycleHostNames.False);
    public static readonly Atom Error = Term.A(ProcLibLifecycleHostNames.Error);
    public static readonly Atom Handler = Term.A(ProcLibLifecycleHostNames.Handler);
    public static readonly Atom Module = Term.A(ProcLibLifecycleHostNames.Module);
    public static readonly Atom Default = Term.A(ProcLibLifecycleHostNames.Default);
    public static readonly MapTerm Empty = new([]);
    public static readonly MapTerm Message = new([new(Term.A(ProcLibLifecycleHostNames.Event), Term.A(ProcLibLifecycleHostNames.Captured))]);
}
