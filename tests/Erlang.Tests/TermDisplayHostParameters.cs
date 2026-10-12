namespace Erlang.Tests;

internal static class TermDisplayHostParameters
{
    public const int NestedTuples = 10000;
    public const byte OpenTuple = (byte)'{';
    public const byte CloseTuple = (byte)'}';
    public const byte Zero = (byte)'0';
    public const byte NewLine = (byte)'\n';
    public const byte CarriageReturn = (byte)'\r';
    public const int FunctionDigestBytes = 16;
    public static readonly byte[] LatinString = [34, 160, 255, 34, 13, 10];
    public const string EmbeddedNewlineModule = "a\nb";
    public static readonly byte[] EmbeddedNewlineFunction = System.Text.Encoding.UTF8.GetBytes("#Fun<a\r\nb.0.0>\r\n");
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
