namespace Erlang;

internal enum NifNativeLoadReasonKind
{
    ModuleMismatch,
    MissingFunction,
    UndeclaredFunction,
    DuplicateFunction,
    LoadVeto,
    IllegalFlags521
}
