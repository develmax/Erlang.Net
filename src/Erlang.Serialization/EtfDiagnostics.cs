namespace Erlang;

internal static class EtfDiagnostics
{
    public static string UnsupportedEncoding(string typeName) => "ETF encoding of " + typeName + " is not implemented";
}
