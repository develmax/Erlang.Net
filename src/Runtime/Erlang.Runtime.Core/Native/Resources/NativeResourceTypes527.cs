namespace Erlang;

internal static class NativeResourceTypes527
{
    private static readonly object gate = new();
    private static readonly Dictionary<nint, NativeResourceType524> types = new();

    internal static void Register527(NativeResourceType524 type)
    {
        lock (gate) types.Add(type.Token, type);
    }

    internal static NativeResourceType524 Resolve527(nint token)
    {
        lock (gate)
            return types.TryGetValue(token, out var type) ? type
                : throw new ArgumentException(NativeResource527Names.BadType);
    }

    internal static void Forget527(NativeResourceType524 type)
    {
        lock (gate)
            if (types.TryGetValue(type.Token, out var entry) && ReferenceEquals(entry, type))
                types.Remove(type.Token);
    }
}
