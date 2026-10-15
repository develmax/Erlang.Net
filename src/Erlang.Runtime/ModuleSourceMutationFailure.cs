namespace Erlang;

internal enum ModuleSourceMutationFailure
{
    StageBusy,
    WriterOrder,
    RuntimeClosing,
    InvalidTicket,
    InvalidSession,
    ReentrantShutdown
}
