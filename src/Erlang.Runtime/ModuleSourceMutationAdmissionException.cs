namespace Erlang;

internal sealed class ModuleSourceMutationAdmissionException : InvalidOperationException
{
    internal ModuleSourceMutationFailure Failure
    {
        get;
    }
    internal ModuleRegistry? Registry
    {
        get;
    }

    internal ModuleSourceMutationAdmissionException(ModuleSourceMutationFailure failure, ModuleRegistry? registry = null)
        : base(failure switch
        {
            ModuleSourceMutationFailure.StageBusy => ModuleSourceMutationDiagnostics.StageBusy,
            ModuleSourceMutationFailure.WriterOrder => ModuleSourceMutationDiagnostics.WriterOrder,
            ModuleSourceMutationFailure.RuntimeClosing => ModuleSourceMutationDiagnostics.RuntimeClosing,
            ModuleSourceMutationFailure.InvalidSession => ModuleSourceMutationDiagnostics.InvalidSession,
            ModuleSourceMutationFailure.ReentrantShutdown => ModuleSourceMutationDiagnostics.ReentrantShutdown,
            _ => ModuleSourceMutationDiagnostics.InvalidTicket
        })
    {
        Failure = failure;
        Registry = registry;
    }
}
