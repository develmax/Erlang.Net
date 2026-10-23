namespace Erlang;

public sealed record SourceFunctionAdmission(object Owner, ModuleCodeImage? Image, SourceCodeLease? Lease)
{
    internal SourceFunctionAdmissionKind Kind { get; init; } = SourceFunctionAdmissionKind.Admitted;
}
