using Erlang;

internal sealed class SupervisorTemporaryLogProbe
{
    internal const string Scenario = "temporary-logs";
    internal const string Error = "error";
    private const string Module = "supervisor_log_probe";
    private const string Observe = "observe";
    private int reports;

    internal int Reports => Volatile.Read(ref reports);

    internal void Register(ModuleRegistry registry) => registry.Register(Module, Observe, 2, (_, args) =>
    {
        if (args[0] is MapTerm entry && entry.TryGet(Term.A("msg"), out var message)
            && message is TupleTerm { Items.Count: 2 } body && body.Items[1] is MapTerm report
            && report.TryGet(Term.A("label"), out var label)
            && Term.Tuple(Term.A(SupervisorModule.Name), Term.A("child_terminated")).Equals(label))
        {
            Interlocked.Increment(ref reports);
        }

        return ValueTask.FromResult<Term>(Term.A("stop"));
    });

    internal async ValueTask<Term> Filter(ProcessContext context)
    {
        Term function = await context.Runtime.Modules.Call(
            context,
            "erlang",
            "make_fun",
            Term.A(Module),
            Term.A(Observe),
            Term.I(2)
        );

        return Term.List(Term.Tuple(Term.A(Module), Term.Tuple(function, Nil.Value)));
    }

    internal int? Expected(Term scenario)
    {
        if (scenario is not TupleTerm { Items.Count: 4 } row
            || row.Items[0] is not Atom { Name: "restart" }
            || row.Items[1] is not Atom { Name: "one_for_one" }
            || row.Items[2] is not Atom { Name: "temporary" })
        {
            return null;
        }

        return row.Items[3] is Atom { Name: "broken" } ? 1 : 0;
    }
}
