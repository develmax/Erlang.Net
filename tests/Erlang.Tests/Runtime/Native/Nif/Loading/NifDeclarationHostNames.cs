namespace Erlang.Tests;

internal static class NifDeclarationHostNames
{
    internal const string Test = "compiler/nif-declaration-control-metadata-and-ownership";
    internal const string Module = "nif_declaration_host";
    internal const string Source = "-module(nif_declaration_host). -export([run/0]). ";
    internal const string Body = "run()->fallback(). fallback()->kept.";
    internal const string Empty = "-nifs([]). ";
    internal const string Repeated = "-nifs([fallback/0,fallback/0]). -nifs([]). -nifs([{fallback,0}]). ";
    internal const string Nifs = "nifs";
    internal const string Fallback = "fallback";
    internal const string Other = "other";
    internal const string Initializer = "DeclaredNifs=";
    internal const string SourcePath = "nif_declaration_host.erl";
    internal const string Broken = "Nif declaration metadata contract failed";
    internal const string FunctionCode = "ERL005";
    internal const string Undefined = "Undefined nif missing/7";
    internal const string UndefinedDeclaration = "-nifs([missing/7]). ";
}
