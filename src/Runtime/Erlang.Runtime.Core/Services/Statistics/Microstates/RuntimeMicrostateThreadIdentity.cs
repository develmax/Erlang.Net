namespace Erlang;

internal sealed class RuntimeMicrostateThreadIdentity
{
    [ThreadStatic] private static RuntimeMicrostateThreadIdentity? current;
    internal int OperatingSystemId { get; } = RuntimeOsThreadIdentity.Current();
    internal static RuntimeMicrostateThreadIdentity Current => current ??= new();
}
