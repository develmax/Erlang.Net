namespace Erlang;

// Tuple layout shared with original gen_statem sys callbacks; includes record tags.
internal static class GenStatemRecordLayout
{
    internal static class SystemState
    {
        internal const int Arity = 2;
        internal const int Parameters = 0;
        internal const int State = 1;
    }
    internal static class Parameters
    {
        internal const int Arity = 7;
        internal const int CallbackMode = 1;
        internal const int StateEnter = 2;
        internal const int Parent = 3;
        internal const int Modules = 4;
        internal const int Name = 5;
        internal const int HibernateAfter = 6;
    }
    internal static class State
    {
        internal const int Arity = 6;
        internal const int StateData = 1;
        internal const int Postponed = 2;
        internal const int Timers = 3;
        internal const int ZeroQueue = 4;
        internal const int Hibernate = 5;
    }
    internal static class StateData
    {
        internal const int Arity = 2;
        internal const int State = 0;
        internal const int Data = 1;
    }
    internal static class StatusArguments
    {
        internal const int Dictionary = 0;
        internal const int Status = 1;
        internal const int Parent = 2;
        internal const int Debug = 3;
        internal const int State = 4;
    }
}
