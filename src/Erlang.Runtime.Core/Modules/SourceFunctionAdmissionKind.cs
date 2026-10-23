namespace Erlang;

internal enum SourceFunctionAdmissionKind
{
    Admitted,
    MissingCurrentLocalFunction,
    Retry
}
