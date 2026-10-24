namespace Erlang.Tests;

public static class MatchSpecHostChecks
{
    public static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        Term variable = Term.A("$1000000");
        Term specification = Cons.From([Term.Tuple(
            Cons.From([variable]),
            Cons.From([Term.Tuple(Term.A("is_integer"), variable)]),
            Cons.From([
                Term.Tuple(Term.A(MatchSpecNames.Message), variable),
                Term.Tuple(Term.A(MatchSpecNames.ReturnTrace)),
                Term.Tuple(Term.A(MatchSpecNames.ExceptionTrace))
            ])
        )]);
        MatchSpecCompilation compilation = MatchSpecCompiler.Compile(specification, MatchSpecDialect.Trace);
        Require(compilation.Diagnostics.Count == 0 && compilation.Program is not null);
        MatchSpecProgram program = compilation.Program!;
        Require(program.RequiredTraceActions.Count == 3);
        var handles = new List<ProcessHandle>();
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        for (int index = 0; index < MatchSpecHostNames.ConcurrentRuns; index++)
        {
            int expected = index;
            handles.Add(runtime.Spawn(async context =>
            {
                try
                {
                    var actions = new DeliveryActions();
                    MatchSpecResult actual = program.Run(
                        context,
                        Cons.From([Term.I(expected)]),
                        MatchSpecRunMode.Production,
                        actions
                    );
                    Require(actual.Matched && actual.Value.Equals(Term.I(expected)) && actual.ReturnTrace && actual.ExceptionTrace);
                    Require(actions.Deliveries == 1 && actions.ReturnRequests == 1 && actions.ExceptionRequests == 1);
                    Term? delivered = await context.ReceiveAsync<Term>(
                        term => term is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A(MatchSpecHostNames.Delivered)) ? tuple.Items[1] : null,
                        TimeSpan.Zero
                    );
                    Require(delivered is not null && delivered.Equals(Term.I(expected)));
                    MatchSpecResult test = program.Run(context, Cons.From([Term.I(expected)]));
                    Require(test.Value.Equals(actual.Value) && actions.Deliveries == 1);
                    Require(await context.ReceiveAsync<Term>(term => term, TimeSpan.Zero) is null);
                    Require(!program.Run(context, Cons.From([Term.A(MatchSpecNames.Table)])).Matched);
                    MatchSpecCompilation unsupported = MatchSpecCompiler.Compile(
                        Cons.From([Term.Tuple(
                        Term.A(MatchSpecNames.Wildcard),
                        Nil.Value,
                        Cons.From([Term.Tuple(Term.A("display"), Term.I(1))])
                    )]),
                        MatchSpecDialect.Trace
                    );
                    Require(unsupported.Program is not null);
                    try
                    {
                        unsupported.Program!.Run(context, Nil.Value);
                        throw new InvalidOperationException(MatchSpecHostNames.Failure);
                    }
                    catch (ErlangException exception)
                    {
                        Require(exception.Reason.Equals(MatchSpecNames.UnsupportedAction("display", 1).Reason));
                    }
                }
                catch (Exception exception)
                {
                    failures.Enqueue(exception);
                }

                return Term.I(expected);
            }));
        }
        await Task.WhenAll(handles.Select(handle => handle.Completion));
        if (failures.TryDequeue(out Exception? failure))
            throw failure;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(MatchSpecHostNames.Failure);
    }

    private sealed class DeliveryActions : IMatchSpecTraceActions
    {
        public int Deliveries;
        public int ReturnRequests;
        public int ExceptionRequests;

        public bool Supports(string name, int arity) => name is MatchSpecNames.Message && arity == 1 || name is MatchSpecNames.ReturnTrace or MatchSpecNames.ExceptionTrace && arity == 0;

        public Term Execute(ProcessContext context, string name, IReadOnlyList<Term> arguments)
        {
            if (name == MatchSpecNames.Message)
            {
                Deliveries++;
                context.Runtime.Send(context.Self, Term.Tuple(Term.A(MatchSpecHostNames.Delivered), arguments[0]));
            }
            else if (name == MatchSpecNames.ReturnTrace)
                ReturnRequests++;
            else if (name == MatchSpecNames.ExceptionTrace)
                ExceptionRequests++;

            return Term.A(ErlangBooleanAtoms.True);
        }
    }
}
