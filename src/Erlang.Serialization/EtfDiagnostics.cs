namespace Erlang;

internal static class EtfDiagnostics
{
    public const string LongReferenceId = "ETF reference IDs longer than 64 bits are not implemented";

    public static string UnsupportedEncoding(string typeName) => "ETF encoding of " + typeName + " is not implemented";

    public static string UnsupportedTag(int tag) => $"ETF tag {tag} is not implemented";
}
