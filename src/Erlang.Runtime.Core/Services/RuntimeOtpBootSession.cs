namespace Erlang;

public sealed record RuntimeOtpBootSession(Pid InitializationProcess, Task<Term> Completion);
