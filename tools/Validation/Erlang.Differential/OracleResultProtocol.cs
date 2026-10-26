namespace Erlang.Differential;

public static class OracleResultProtocol
{
    public const string Begin = "ERLANG_NET_ETF_BEGIN:";
    public const string End = ":ERLANG_NET_ETF_END";

    public static string Command(string variable) => "io:format(\"~n" + Begin + "~s" + End + "~n\",[base64:encode(term_to_binary(" + variable + "))]),halt().";

    public static Term Decode(string output)
    {
        int begin = output.IndexOf(Begin, StringComparison.Ordinal);
        int end = output.IndexOf(End, StringComparison.Ordinal);
        if (begin < 0 || end < begin + Begin.Length || output.IndexOf(Begin, begin + Begin.Length, StringComparison.Ordinal) >= 0 || output.IndexOf(End, end + End.Length, StringComparison.Ordinal) >= 0)
            throw new InvalidOperationException(OracleDiagnostics.InvalidResultFrame);

        return ExternalTermFormat.Decode(Convert.FromBase64String(output.Substring(begin + Begin.Length, end - begin - Begin.Length)));
    }
}
