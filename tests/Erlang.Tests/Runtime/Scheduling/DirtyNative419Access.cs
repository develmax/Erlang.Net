using System.Reflection;
using System.Runtime.CompilerServices;
namespace Erlang.Tests;

internal static class DirtyNative419Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DirtyNative419Names.Failure);
    }

    internal static async Task<Term> Schedule(
        ProcessContext context,
        string kind,
        IReadOnlyList<Term> arguments,
        Func<ProcessContext, IReadOnlyList<Term>, Term> body
    )
    {
        Assembly assembly = typeof(ProcessRuntime).Assembly;
        object value = Enum.Parse(assembly.GetType(DirtyNative419Names.Kind, true)!, kind);
        object pending = typeof(ProcessRuntime).GetMethod(DirtyNative419Names.Schedule, DirtyNative419Names.Members)!.Invoke(context.Runtime, [context, value, DirtyNative419Names.Runtime, DirtyNative419Names.Schedule, arguments, body, null])!;

        return await (ValueTask<Term>)pending;
    }

    internal static string Kind(ProcessContext context) => (typeof(ProcessRuntime).Assembly.GetType(DirtyNative419Names.Thread, true)!.GetMethod(DirtyNative419Names.Read, DirtyNative419Names.Members)!.Invoke(null, [context])!).ToString()!;

    internal static Term NativeSchedulerIdentity(ProcessContext context)
    {
        object?[] arguments = [context, Term.A(DirtyNative419Names.SchedulerIdentity), null];
        Require((bool)typeof(ProcessRuntime).GetMethod(DirtyNative419Names.ReadScheduler, DirtyNative419Names.Members)!.Invoke(context.Runtime, arguments)!);

        return (Term)arguments[2]!;
    }

    internal static bool NormalTurn(ProcessContext context) => (bool)typeof(ProcessRuntime).Assembly.GetType(DirtyNative419Names.Turn, true)!.GetMethod(DirtyNative419Names.IsOwned, DirtyNative419Names.Members)!.Invoke(null, [context])!;

    internal static object Snapshot(ProcessRuntime runtime)
    {
        object owner = typeof(ProcessRuntime).GetField(DirtyNative419Names.Owner, DirtyNative419Names.Members)!.GetValue(runtime)!;

        return owner.GetType().GetMethod(DirtyNative419Names.Snapshot, DirtyNative419Names.Members)!.Invoke(owner, null)!;
    }

    internal static T Field<T>(object snapshot, string field) => (T)snapshot.GetType().GetProperty(field, DirtyNative419Names.Members)!.GetValue(snapshot)!;

    internal static void Cancel(ProcessContext context) => DebugLocks419Access.Cancel(context);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static (Task<Term> Work, WeakReference<Term> Argument, WeakReference<object> Ambient) CreateWeakWork(ProcessContext context)
    {
        Term argument = Term.Tuple(Term.List(Term.I(771), Term.I(772)));
        object ambient = new();
        var a = new WeakReference<Term>(argument);
        var e = new WeakReference<object>(ambient);
        object? previous = DirtyNative419Ambient.Value.Value;
        try
        {
            DirtyNative419Ambient.Value.Value = ambient;
            Task<Term> work = Schedule(
                context,
                DirtyNative419Names.Cpu,
                [argument],
                static (_, _) => Nil.Value
            );

            return (work, a, e);
        }
        finally
        {
            DirtyNative419Ambient.Value.Value = previous;
        }
    }
}
