namespace Erlang;

internal sealed record RuntimeActorContinuation(Action Body, ProcessExecutionAdmission Admission);
