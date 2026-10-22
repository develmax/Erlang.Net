namespace Erlang;

internal sealed class ManagedCollectorBinding413
{
    internal ProcessRuntime Owner { get; }
    internal ErlangFunction Body { get; }
    internal string Module { get; }
    internal string Function { get; }
    internal int Arity { get; }

    internal ManagedCollectorBinding413(
        ProcessRuntime owner,
        string module,
        string function,
        int arity
    )
    {
        Owner = owner;
        Module = module;
        Function = function;
        Arity = arity;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Owner.AdmitManagedCollector413(context);
        if (arguments.Count != Arity) throw new ArgumentException(ManagedCollectorNames413.Foreign);
        try
        {
            Term result = (Module, Function) switch
            {
                (ManagedCollectorNames413.LiteralModule, ManagedCollectorNames413.Release) => Owner.CollectorRelease413(context),
                (ManagedCollectorNames413.LiteralModule, ManagedCollectorNames413.Copy) => Owner.CollectorCopyRequest413(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ),
                (ManagedCollectorNames413.TraceModule, ManagedCollectorNames413.Check) => Owner.CollectorCheck413(context),
                _ => Owner.CollectorTraceSignal413(context, arguments[0])
            };

            return ValueTask.FromResult(result);
        }
        catch (ErlangException error)
        {
            throw context.CaptureNativeFailure(
                error,
                Module,
                Function,
                arguments
            );
        }
    }
}
