namespace Erlang;

internal static class ModuleSourceMutationDiagnostics
{
    internal const string StageBusy = "Source code staging is occupied by another operation.";
    internal const string WriterOrder = "Source staging cannot wait for or adopt an unrelated retained code writer.";
    internal const string RuntimeClosing = "Source code admission is closing.";
    internal const string InvalidTicket = "Source staging ticket identity, generation or execution scope is invalid.";
    internal const string InvalidSession = "Purge session identity, generation or state is invalid.";
    internal const string ReentrantShutdown = "Source session shutdown cannot be joined while the registry monitor is retained.";
}
