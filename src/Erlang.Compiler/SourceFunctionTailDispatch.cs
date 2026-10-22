using System.Runtime.CompilerServices;

namespace Erlang.Compiler;

internal static class SourceFunctionTailDispatch
{
    private static readonly AsyncLocal<SourceFunctionTailScope?> Pending = new();
    private static readonly ConditionalWeakTable<FunctionTerm, object> SourceFunctions = new();
    private static readonly object Marker = new();

    internal static void Register(FunctionTerm function)
    {
        SourceFunctions.GetValue(function, _ => Marker);
    }

    internal static bool IsCompilerSource(FunctionTerm function) => SourceFunctions.TryGetValue(function, out _);

    internal static bool IsSource(ProcessContext context, FunctionTerm function)
    {
        if (SourceFunctions.TryGetValue(function, out _))
        {
            return true;
        }

        return function.LocalIdentity is { } identity
            && context.Runtime.Functions.Capture(identity, function.Arity, function).Publication.IsSourceFactory;
    }

    internal static SourceFunctionTailScope Enter(ProcessContext context)
    {
        var scope = new SourceFunctionTailScope(context, Pending.Value);
        Pending.Value = scope;

        return scope;
    }

    internal static void Restore(SourceFunctionTailScope? previous)
    {
        Pending.Value = previous;
    }

    internal static void Cancel(ProcessContext context)
    {
        if (Pending.Value is { } scope && ReferenceEquals(scope.Context, context))
        {
            Pending.Value = scope.Previous;
        }
    }

    internal static void Transfer(ProcessContext context, SourceInvocationTarget target)
    {
        if (Pending.Value is { } scope && ReferenceEquals(scope.Context, context))
        {
            Pending.Value = scope.Previous;
            throw new SourceFunctionTransfer(scope, target);
        }
    }
}
