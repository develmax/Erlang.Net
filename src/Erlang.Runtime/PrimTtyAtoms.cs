namespace Erlang;

internal static class PrimTtyAtoms
{
    internal static readonly Atom Ok = Term.A(PrimTtyNames.Ok), Error = Term.A(PrimTtyNames.Error);
    internal static readonly Atom NoSupport = Term.A(PrimTtyNames.NoSupport), Closed = Term.A(PrimTtyNames.Closed);
    internal static readonly Atom True = Term.A(PrimTtyNames.True), False = Term.A(PrimTtyNames.False);
    internal static readonly Atom Select = Term.A(PrimTtyNames.Select), ReadyInput = Term.A(PrimTtyNames.ReadyInput);

    internal static Term Boolean(bool value) => value ? True : False;

    internal static Term Failure(Term reason) => Term.Tuple(Error, reason);
}
