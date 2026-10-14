namespace Erlang;

public sealed record BeamOnLoadCallbackOutcome(Pid Callback, Term? Returned, Exception? Failure);
