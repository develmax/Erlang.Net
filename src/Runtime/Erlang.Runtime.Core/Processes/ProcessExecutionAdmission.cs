namespace Erlang;

internal enum ProcessExecutionAdmission
{
    Continuation,
    Reduction,
    StackUnwind,
    Reentry
}
