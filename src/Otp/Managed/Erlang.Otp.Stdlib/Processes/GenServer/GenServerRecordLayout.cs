namespace Erlang;

internal static class GenServerRecordLayout
{
    internal static class ServerData
    {
        internal const int Arity = 10;
        internal const int Parent = 1;
        internal const int TimerTag = 2;
        internal const int Name = 3;
        internal const int Module = 4;
        internal const int HibernateAfter = 5;
    }

    internal static class SystemState
    {
        internal const int Arity = 4;
        internal const int ServerData = 0;
        internal const int State = 1;
        internal const int Action = 2;
        internal const int Timer = 3;
    }
}
