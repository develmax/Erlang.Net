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
    public const string FirstMapAtom = "z_map_display_internal_order_host";
    public const string SecondMapAtom = "a_map_display_internal_order_host";
    public static readonly byte[] InternalMapOrder = System.Text.Encoding.UTF8.GetBytes("#{0=>0,z_map_display_internal_order_host=>1,a_map_display_internal_order_host=>2}\r\n");
    public static readonly byte[] EmbeddedNewlineFunction = System.Text.Encoding.UTF8.GetBytes("#Fun<a\r\nb.0.0>\r\n");
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
