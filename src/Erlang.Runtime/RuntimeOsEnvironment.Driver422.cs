namespace Erlang;

internal sealed partial class RuntimeOsEnvironment
{
    internal string? ReadDriver422(byte[] nativeKey)
    {
        string? key = WindowsDriverAcp422.Decode(nativeKey);
        if (key is null) return null;
        lock (gate)
        {
            RequireOpen();

            return tree.Get(key);
        }
    }

    internal int WriteDriver422(byte[] nativeKey, byte[] nativeValue)
    {
        string? key = WindowsDriverAcp422.Decode(nativeKey), value = WindowsDriverAcp422.Decode(nativeValue);
        if (key is null || value is null) return -1;
        lock (gate)
        {
            RequireOpen();
            tree.Put(key, value);

            return 0;
        }
    }
}
