namespace Erlang.Differential;

public static class ZipGeneratorDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("zip-source-dependency", "-module(scope_zip_source). -export([run/0]). run()->[Y || X <- [] && Y <- [X]].", "X"),
        new(
            "zip-source-private",
            "-module(scope_zip_source_private). -export([run/0]). run()->[T || X <- begin T=[],T end && Y <- []].",
            "T"
        ),
        new("zip-key-scope", "-module(scope_zip_key). -export([run/0]). run()->[V || K <- [a] && #{K:=V} <- []].", "K"),
        new("zip-size-scope", "-module(scope_zip_size). -export([run/0]). run()->[X || N <- [8] && <<X:N>> <= <<>>].", "N"),
        new("zip-generator-private", "-module(scope_zip_private). -export([run/0]). run()->[X || X <- [] && Y <- []],X.", "X"),
        new("zip-template-private", "-module(scope_zip_template). -export([run/0]). run()->[Z=1 || X <- [] && Y <- []],Z.", "Z")
    ];
}
