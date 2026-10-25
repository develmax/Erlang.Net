namespace Erlang.NifNativeBackend;

internal enum NifNativePayloadQueryPhase
{
    VetoClosed,
    Loaded,
    AdmissionHeld,
    RetirementPending,
    Unloaded
}
