namespace Erlang.Differential;

public static class CompiledDiagnosticCases
{
    public static IReadOnlyList<CompiledDiagnosticCase> All { get; } = [
        new("bit-size-to-next-size", "-module(scope_size_size). -export([run/0]). run()->B = <<1:(S=4),2:S>>,{B,S}.", "S"),
        new("bit-value-to-own-size", "-module(scope_value_size). -export([run/0]). run()-><<(S=4):S>>.", "S"),
        new("bit-value-to-next-value", "-module(scope_value_value). -export([run/0]). run()-><<(V=1),V>>.", "V"),
        new("bit-size-to-next-value", "-module(scope_size_value). -export([run/0]). run()-><<1:(S=4),S>>.", "S"),
        new("bit-string-size-to-next-size", "-module(scope_string_size). -export([run/0]). run()-><<\"ab\":(S=8),42:S>>.", "S")
    ];
}
