namespace Erlang.NifNativeBackend;

public static class NifPrintf791HostNames
{
    public const string RenderTerm = "native-nif/snprintf791-fixed-term-render-budget-bytes-and-cleanup";
    internal const string Ok = "ok";
    internal const string Module = "snprintf791_fixed_host";
    internal const string Failure = "The fixed NIF printf term-render callback contract failed.";
    internal const string TupleText = "{ok,42}";
    internal const string TextMismatch = "Fixed NIF term text differs: expected={0}; actual={1}.";
    internal const string TruncatedText = "... ";
    internal const string FirstNodeText = "{... ";
    internal const string QuoteAtom = "space atom";
    internal const string QuoteText = "'space atom'";
    internal const string PrintableText = "\"abc\"";
    internal const long DefaultTermBudget = 100000;
}
