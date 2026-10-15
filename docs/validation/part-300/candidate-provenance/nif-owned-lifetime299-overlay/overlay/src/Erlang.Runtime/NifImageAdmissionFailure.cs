namespace Erlang;

internal enum NifImageAdmissionFailure
{
    ForeignRuntime,
    NoExecutingCaller,
    OldCode,
    UnknownImage,
    MissingDescriptor,
    OwnerMismatch,
    PendingCurrentAmbiguous,
    Reload,
    UpgradeUnsupported,
    GenerationChanged,
    ReservationAbandoned,
    ReentrantInitializationOperation,
    RuntimeClosing,
    ReentrantRetirementOperation
}
