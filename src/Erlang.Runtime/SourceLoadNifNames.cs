namespace Erlang;

internal static class SourceLoadNifNames
{
    internal const string Function = "load_nif";
    internal const int Arity = 2;
    internal const string InvalidCapability = "The direct load_nif caller capability is unavailable, consumed, or belongs to another process.";
    internal const string HandlerAlreadyInstalled = "The direct source load_nif handler is already installed.";
}
