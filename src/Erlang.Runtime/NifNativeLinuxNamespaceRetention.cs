using System.Collections.Concurrent;

namespace Erlang;

internal static class NifNativeLinuxNamespaceRetention
{
    private static readonly ConcurrentBag<object> FailedOwners = new();

    internal static void Retain(object owner) => FailedOwners.Add(owner);
}
