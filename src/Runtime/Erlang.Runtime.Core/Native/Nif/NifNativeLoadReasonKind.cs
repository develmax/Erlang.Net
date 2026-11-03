namespace Erlang;

internal enum NifNativeLoadReasonKind
{
    ModuleMismatch,
    MissingFunction,
    UndeclaredFunction,
    DuplicateFunction,
    LoadVeto,
    FutureVersion651,
    OldVersion651,
    Variant651,
    IllegalFlags521
}
