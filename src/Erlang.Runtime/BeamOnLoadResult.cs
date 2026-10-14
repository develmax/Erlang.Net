namespace Erlang;

public sealed record BeamOnLoadResult(
    bool Published,
    Pid Callback,
    BeamOnLoadCallbackOutcome Outcome,
    Term DownReason
);
