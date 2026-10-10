namespace Erlang.Differential;

public static class MapComprehensionDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("mg-generator-private", "-module(scope_mg_private). -export([run/0]). run()->[K || K := V <- #{}],K.", "K"),
        new("mg-source-private", "-module(scope_mg_source). -export([run/0]). run()->[M || K := V <- begin M=#{1=>2},M end].", "M"),
        new("mc-key-binding-private", "-module(scope_mc_key). -export([run/0]). run()->#{(X=1)=>X || true}.", "X"),
        new("mc-fields-isolated", "-module(scope_mc_fields). -export([run/0]). run()->#{(X=1)=>1,a=>X || true}.", "X"),
        new("mc-not-exported", "-module(scope_mc_export). -export([run/0]). run()->#{(X=1)=>1 || true},X.", "X"),
        new("mg-unbound-key", "-module(scope_mg_key). -export([run/0]). run()->[V || K := #{Missing:=V} <- #{}].", "Missing")
    ];
}
