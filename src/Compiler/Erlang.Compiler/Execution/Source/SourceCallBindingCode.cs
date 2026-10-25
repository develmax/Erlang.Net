using System.Text.Json;

namespace Erlang.Compiler;

internal static class SourceCallBindingCode
{
    public static string Emit(SourceCallBindings bindings)
    {
        var suppressed = string.Join(',', bindings.Suppressed.Select(value => $"({JsonSerializer.Serialize(value.Name)},{value.Arity})"));
        var imports = string.Join(
            ',',
            bindings.Imports.Select(value => $"({JsonSerializer.Serialize(value.Module)},{JsonSerializer.Serialize(value.Name)},{value.Arity})")
        );

        return "CallBindings=new global::Erlang.Compiler.SourceCallBindings(" + (bindings.SuppressAll ? "true" : "false") + ",new (string Name,int Arity)[]{" + suppressed + "},new (string Module,string Name,int Arity)[]{" + imports + "})";
    }
}
