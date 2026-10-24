namespace Erlang.Tests;

internal sealed record QueueBeamErrorObservation(
    bool BeamBackend,
    string RequestedClass,
    string ActualClass,
    Term Reason,
    Term RawStack
);
