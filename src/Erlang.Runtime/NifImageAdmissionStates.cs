namespace Erlang;

internal static class NifImageAdmissionStates
{
    internal const int Reserved = 0;
    internal const int Initializing = 1;
    internal const int Committing = 2;
    internal const int AbandonRequested = 3;
    internal const int Closed = 4;
}
