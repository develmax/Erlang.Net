using System.Globalization;
using System.Runtime.CompilerServices;

namespace Erlang.Compiler;

internal static class AnonymousFunctionFrames
{
    private sealed record Entry(string Name);

    private const string InterpretedOwner = "erl_eval";
    private const string AnonymousOwner = "anonymous";
    private const string EntrySeparator = "-fun-";
    private static readonly ConditionalWeakTable<Expr.Fun, Entry> Entries = new();
    private static long next;

    public static string Module(ModuleDefinition? module) => module?.Name ?? InterpretedOwner;

    public static string Name(Expr.Fun expression, ProcessContext context) => Entries.GetValue(
        expression,
        _ => new Entry(
        "-" + (context.CurrentFunction ?? AnonymousOwner) + "/" + context.CurrentFunctionArity.ToString(CultureInfo.InvariantCulture) + EntrySeparator + Interlocked.Increment(ref next).ToString(CultureInfo.InvariantCulture) + "-"
    )
    ).Name;
}
