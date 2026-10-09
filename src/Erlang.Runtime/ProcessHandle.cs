

namespace Erlang;

public sealed record ProcessHandle(Pid Pid, Task<Term> Completion);
