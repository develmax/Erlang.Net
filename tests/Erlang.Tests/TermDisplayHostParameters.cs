namespace Erlang.Tests;

internal static class TermDisplayHostParameters
{
    public const int NestedTuples = 10000;
    public const byte OpenTuple = (byte)'{';
    public const byte CloseTuple = (byte)'}';
    public const byte Zero = (byte)'0';
    public const byte NewLine = (byte)'\n';
    public static readonly byte[] LatinString = [34, 160, 255, 34, 10];
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
