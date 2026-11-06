namespace Erlang;

internal static class LoggerDisplay
{
    internal const string Io = "io";
    internal const string Getopts = "getopts";
    internal const string Latin1 = "latin1";
    internal const string UnicodeModifier = "t";
    internal const string PrimaryHeading = "Primary configuration: ~n";
    internal const string PrimaryLevel = "    Level: ~p~n";
    internal const string PrimaryDefault = "    Filter Default: ~p~n";
    internal const string PrimaryFilters = "    Filters: ~n";
    internal const string HandlerHeading = "Handler configuration: ~n";
    internal const string ProxyHeading = "Proxy configuration: ~n";
    internal const string ModulesHeading = "Level set per module: ~n";
    internal const string FilterFormat = "~sId: ~{0}p~n~s    Fun: ~{0}p~n~s    Arg: ~{0}p~n";
    internal const string NoFilters = "~s(none)~n";
    internal const string HandlerFormat = "    Id: ~{0}p~n        Module: ~p~n        Level:  ~p~n        Formatter:~n            Module: ~p~n            Config:~n";
    internal const string HandlerFilters = "        Filter Default: ~p~n        Filters:~n";
    internal const string HandlerConfigHeading = "        Handler Config:~n";
    internal const string CustomConfigHeading = "        Custom Config:~n";
    internal const string None = "    (none)~n";
    internal const string PairFormat = "~s~{0}p: ~{0}p~n";
    internal const string ValueFormat = "~s~{0}p~n";
    internal const string ModuleFormat = "    Module: ~{0}p~n        Level: ~p~n";
    internal const string Indent = "    ";
    internal const string FilterIndent = "        ";
    internal const string HandlerIndent = "            ";
    internal const string FormatterIndent = "                ";
    internal const string InvalidConfig = "Invalid logger config: ~p";
    internal const string InvalidDefaultConfig = "Invalid default handler config: ~p";
    internal const string InvalidHandlerConfig = "Invalid logger handler config: ~p";
}
