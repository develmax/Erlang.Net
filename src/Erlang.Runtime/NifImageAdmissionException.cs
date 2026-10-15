namespace Erlang;

// Internal coordination failure, not an Erlang load_nif error contract.
internal sealed class NifImageAdmissionException(NifImageAdmissionFailure failure) : InvalidOperationException(failure.ToString())
{
    internal NifImageAdmissionFailure Failure { get; } = failure;
}
