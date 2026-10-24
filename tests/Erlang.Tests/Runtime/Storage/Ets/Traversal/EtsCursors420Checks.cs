using System.Reflection;
using Erlang;
namespace Erlang.Tests;

internal static class EtsCursors420Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsCursors420Names.Failure);
    }

    private static void Install(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveProviders.Register(runtime, runtime.Modules, RuntimeNativePrimitives.EtsStorageCore);
        RuntimeBootstrapEtsCompiled419.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsFixation420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsCursor420.Register(runtime, runtime.Modules);
    }

    private static ValueTask<Term> Call(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        params Term[] arguments
    ) => runtime.Modules.Call(
        context,
        EtsCursors420Names.Module,
        function,
        arguments
    );

    private static Term Spec() => Term.List(Term.Tuple(Term.A(EtsCursors420Names.Wildcard), Nil.Value, Term.List(Term.A(EtsCursors420Names.Whole))));

    private static Term Rows() => Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.Tuple(Term.I(2), Term.I(20)), Term.Tuple(Term.I(3), Term.I(30)));

    private static async Task<Term> Create(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, string kind) => await actor.InvokeAsync(async context => await Call(
        runtime,
        context,
        EtsCursors420Names.New,
        Term.A(EtsCursors420Names.Name),
        Term.List(Term.A(EtsCursors420Names.Named), Term.A(EtsCursors420Names.Public), Term.A(kind))
    ));

    internal static async Task Binding()
    {
        await using var runtime = new ProcessRuntime(1);
        foreach (var signature in EtsCursors420Names.Signatures) Require(!runtime.Modules.HasExport(EtsCursors420Names.Module, signature.Function, signature.Arity));
        RuntimeBootstrapEtsCursor420.Register(runtime, runtime.Modules);
        RuntimeBootstrapEtsCursor420.Register(runtime, runtime.Modules);
        foreach (var signature in EtsCursors420Names.Signatures) Require(runtime.Modules.HasExport(EtsCursors420Names.Module, signature.Function, signature.Arity) && runtime.Modules.SourceOwner(EtsCursors420Names.Module, signature.Function, signature.Arity) is null);
        await using var foreign = new ProcessRuntime(1);
        bool refused = false;
        try
        {
            RuntimeBootstrapEtsCursor420.Register(runtime, foreign.Modules);
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
        await using var rollback = new ProcessRuntime(1);
        var error = new InvalidOperationException(EtsCursors420Names.Failure);
        try
        {
            await rollback.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapEtsCursor420.Register(rollback, registry);
                throw error;
            });
        }
        catch (InvalidOperationException seen) when (ReferenceEquals(seen, error)) { }
        foreach (var signature in EtsCursors420Names.Signatures) Require(!rollback.Modules.HasExport(EtsCursors420Names.Module, signature.Function, signature.Arity));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        foreach (var signature in EtsCursors420Names.Signatures.Where(signature => signature.Arity == 1)) Require((await actor.InvokeAsync(async context => await Call(
            runtime,
            context,
            signature.Function,
            Term.A(EtsCursors420Names.End)
        ))).Equals(Term.A(EtsCursors420Names.End)));
    }

    internal static async Task Tree()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await Create(runtime, actor, EtsCursors420Names.Ordered);
        await actor.InvokeAsync(async context =>
        {
            _ = await Call(
                runtime,
                context,
                EtsCursors420Names.Insert,
                table,
                Rows()
            );
            var first = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Select,
                table,
                Spec(),
                Term.I(1)
            );
            var cursor = (TupleTerm)first.Items[1];
            Require(first.Items[0].Equals(Term.List(Term.Tuple(Term.I(1), Term.I(10)))) && cursor.Items.Count == 8 && cursor.Items[0].Equals(table) && cursor.Items[1].Equals(Term.I(1)) && cursor.Items[2] is Nil && cursor.Items[3].Equals(Term.I(1)) && cursor.Items[5] is Nil && cursor.Items[6].Equals(Term.I(0)) && cursor.Items[7].Equals(Term.I(0)));
            Require((await Call(
                runtime,
                context,
                EtsCursors420Names.Compiled,
                cursor.Items[4]
            )).Equals(Term.A(EtsCursors420Names.True)));
            var second = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Object,
                cursor
            );
            Require(second.Items[0].Equals(Term.List(Term.Tuple(Term.I(2), Term.I(20)))));
            var third = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Match,
                second.Items[1]
            );
            Require(third.Items[0].Equals(Term.List(Term.Tuple(Term.I(3), Term.I(30)))));
            Require((await Call(
                runtime,
                context,
                EtsCursors420Names.Reverse,
                third.Items[1]
            )).Equals(Term.A(EtsCursors420Names.End)));
            Term raw = Term.Tuple(
                table,
                Term.I(2),
                Nil.Value,
                Term.I(0),
                cursor.Items[4],
                Term.List(Term.A(EtsCursors420Names.Marker)),
                Term.I(0),
                Term.I(0)
            );
            Require((await Call(
                runtime,
                context,
                EtsCursors420Names.Select,
                raw
            )).Equals(Term.List(Term.Tuple(Term.I(1), Term.I(10)), Term.A(EtsCursors420Names.Marker))));
            var bound = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Select,
                table,
                Term.List(Term.Tuple(Term.Tuple(Term.I(2), Term.A(EtsCursors420Names.Wildcard)), Nil.Value, Term.List(Term.A(EtsCursors420Names.Whole)))),
                Term.I(1)
            );
            Require(bound.Items[1].Equals(Term.A(EtsCursors420Names.End)));

            return true;
        });
    }

    internal static async Task Hash()
    {
        await using var runtime = new ProcessRuntime(1);
        Install(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await Create(runtime, actor, EtsCursors420Names.Set);
        await actor.InvokeAsync(async context =>
        {
            Term resource = await Call(
                runtime,
                context,
                EtsCursors420Names.Compile,
                Spec()
            );
            Term a = Term.A(EtsCursors420Names.A), b = Term.A(EtsCursors420Names.B), c = Term.A(EtsCursors420Names.C);
            Term raw = Term.Tuple(
                table,
                Term.I(-1),
                Term.I(1),
                resource,
                Term.List(a, b, c),
                Term.I(3)
            );
            var first = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Select,
                raw
            );
            var cursor = (TupleTerm)first.Items[1];
            Require(first.Items[0].Equals(Term.List(c)) && cursor.Items.Count == 6 && cursor.Items[0].Equals(table) && cursor.Items[1].Equals(Term.I(-1)) && cursor.Items[2].Equals(Term.I(1)) && cursor.Items[3].Equals(resource) && cursor.Items[4].Equals(Term.List(b, a)) && cursor.Items[5].Equals(Term.I(2)));
            var second = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Object,
                cursor
            );
            Require(second.Items[0].Equals(Term.List(a)));
            var third = (TupleTerm)await Call(
                runtime,
                context,
                EtsCursors420Names.Reverse,
                second.Items[1]
            );
            Require(third.Items[0].Equals(Term.List(b)) && third.Items[1].Equals(Term.A(EtsCursors420Names.End)));
            Term improper = new Cons(a, Term.A(EtsCursors420Names.Tail));
            Require(ReferenceEquals(
                await Call(
                    runtime,
                    context,
                    EtsCursors420Names.Match,
                    Term.Tuple(
                        table,
                        Term.I(-2),
                        Term.I(0),
                        resource,
                        improper,
                        Term.I(0)
                    )
                ),
                improper
            ));
            Require((await Call(
                runtime,
                context,
                EtsCursors420Names.Select,
                Term.Tuple(
                    table,
                    Term.I(-1),
                    Term.I(1),
                    resource,
                    Term.List(a),
                    Term.I(1),
                    Term.I(2)
                )
            )).Equals(Term.Tuple(Term.List(a), Term.A(EtsCursors420Names.End))));

            return true;
        });
    }

    internal static async Task Mutable()
    {
        await using var runtime = new ProcessRuntime(2);
        Install(runtime);
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await using var writer = new RuntimeNodeFoundationHostActor(runtime);
        Term table = await Create(runtime, owner, EtsCursors420Names.Ordered);
        object storage = EtsFoundation405Access.Get(runtime);
        object gate = typeof(ProcessRuntime).GetField(EtsCursors420Names.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
        _ = await owner.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsCursors420Names.Erlang,
            EtsCursors420Names.ProcessFlag,
            Term.A(EtsCursors420Names.Scheduler),
            Term.I(1)
        ));
        _ = await writer.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            EtsCursors420Names.Erlang,
            EtsCursors420Names.ProcessFlag,
            Term.A(EtsCursors420Names.Scheduler),
            Term.I(2)
        ));
        _ = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCursors420Names.Insert,
            table,
            Rows()
        ));
        runtime.Modules.DeleteModule(EtsCursors420Names.Erlang);
        int invoked = 0;
        runtime.Modules.Register(
            EtsCursors420Names.Erlang,
            EtsCursors420Names.IsInteger,
            1,
            (callbackContext, arguments) =>
        {
            Require(!Monitor.IsEntered(gate));
            if (Interlocked.Increment(ref invoked) == 1)
            {
                Task operation;
                using (ExecutionContext.SuppressFlow()) operation = Task.Run(async () =>
          {
              _ = await writer.InvokeAsync(async context => await Call(
                  runtime,
                  context,
                  EtsCursors420Names.Delete,
                  table,
                  Term.I(2)
              ));
          });
                operation.GetAwaiter().GetResult();
                Require((long)EtsFoundation405Access.Snapshot(storage)[3]! == 2);
            }

            return ValueTask.FromResult<Term>(Term.A(EtsCursors420Names.True));
        }
        );
        Term spec = Term.List(Term.Tuple(
            Term.Tuple(Term.A(EtsCursors420Names.Variable), Term.A(EtsCursors420Names.Wildcard)),
            Term.List(Term.Tuple(Term.A(EtsCursors420Names.IsInteger), Term.A(EtsCursors420Names.Variable))),
            Term.List(Term.A(EtsCursors420Names.Whole))
        ));
        Term result = await owner.InvokeAsync(async context => await Call(
            runtime,
            context,
            EtsCursors420Names.Select,
            table,
            spec,
            Term.I(3)
        ));
        Require(((TupleTerm)result).Items[0].Equals(Rows()) && invoked == 3);
    }
}
