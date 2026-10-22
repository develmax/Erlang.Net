namespace Erlang.Tests;

internal static class EtsCompiled419Checks
{
    internal static async Task Basic()
    {
        await using var runtime = new ProcessRuntime(1);
        EtsCompiled419Access.Require(!runtime.Modules.HasExport(EtsCompiled419Names.Module, EtsCompiled419Names.Compile, 1));
        await EtsCompiled419Access.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, EtsCompiled419Access.IdentitySpec);
            Term second = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, EtsCompiled419Access.IdentitySpec);
            EtsCompiled419Access.Require(first is ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.CompiledMatchSpecification419 }
                && !first.Equals(second)
                && (await EtsCompiled419Access.Call(context, EtsCompiled419Names.IsCompiled, first)).Equals(Term.A(EtsCompiled419Names.True)));
            var identity = (ReferenceTerm)first;
            var plain = new ReferenceTerm(identity.Node, identity.Words, identity.Creation);
            var wrong = runtime.ReferenceResources.CreateBinary(new byte[] { 1 });
            foreach (Term invalid in new Term[] { plain, wrong, Term.I(1), Nil.Value, new BitString([1]) })
                EtsCompiled419Access.Require((await EtsCompiled419Access.Call(context, EtsCompiled419Names.IsCompiled, invalid)).Equals(Term.A(EtsCompiled419Names.False)));
            Term tail = Term.A(EtsCompiled419Names.Tail);
            EtsCompiled419Access.Require(ReferenceEquals(await EtsCompiled419Access.Call(
                context,
                EtsCompiled419Names.Run,
                Nil.Value,
                first,
                tail
            ), tail));
            Term reversed = await EtsCompiled419Access.Call(
                context,
                EtsCompiled419Names.Run,
                Term.List(Term.I(1), Term.I(2)),
                first,
                tail
            );
            EtsCompiled419Access.Require(reversed.Equals(new Cons(Term.I(2), new Cons(Term.I(1), tail))));
            int before = runtime.ReferenceResources.LiveResources;
            bool failed = false;
            try
            {
                _ = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, Nil.Value);
            }
            catch (ErlangException error)
            {
                TupleTerm frame = EtsCompiled419Access.First(error);
                failed = frame.Items[1].Equals(Term.A(EtsCompiled419Names.Compile)) && frame.Items[2].Equals(Term.List(Nil.Value));
            }
            EtsCompiled419Access.Require(failed && runtime.ReferenceResources.LiveResources == before);

            return true;
        });
        await using var rollback = new ProcessRuntime(1);
        bool refused = false;
        ErlangFunction previous = (_, _) => ValueTask.FromResult<Term>(Nil.Value);
        await rollback.InitializeModulesAsync(registry =>
        {
            registry.Register(
                EtsCompiled419Names.Module,
                EtsCompiled419Names.IsCompiled,
                1,
                previous
            );
            try
            {
                RuntimeBootstrapEtsCompiled419.Register(rollback, registry);
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            EtsCompiled419Access.Require(refused
                && !registry.HasExport(EtsCompiled419Names.Module, EtsCompiled419Names.Compile, 1)
                && ReferenceEquals(EtsCompiled419Access.Body(rollback, EtsCompiled419Names.IsCompiled, 1), previous));
        });
    }

    internal static async Task Canonical()
    {
        await using var runtime = new ProcessRuntime(1);
        await EtsCompiled419Access.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term spec = Term.List(Term.Tuple(
            Term.A(EtsCompiled419Names.Variable),
            Term.List(Term.Tuple(Term.A(EtsCompiled419Names.IntegerPredicate), Term.A(EtsCompiled419Names.Variable))),
            Term.List(Term.A(EtsCompiled419Names.Whole))
        ));
        _ = runtime.Modules.DeleteModule(EtsCompiled419Names.Runtime);
        runtime.Modules.Register(
            EtsCompiled419Names.Runtime,
            EtsCompiled419Names.IntegerPredicate,
            1,
            (_, _) => ValueTask.FromResult<Term>(Term.A(EtsCompiled419Names.False))
        );
        await actor.InvokeAsync(async context =>
        {
            EtsCompiled419Access.Require((await runtime.Modules.Call(
                context,
                EtsCompiled419Names.Runtime,
                EtsCompiled419Names.IntegerPredicate,
                Term.I(1)
            ))
                .Equals(Term.A(EtsCompiled419Names.False)));
            var old = MatchSpecCompiler.Compile(spec, MatchSpecDialect.Table).Program!;
            EtsCompiled419Access.Require(!old.Run(context, Term.I(1)).Matched);
            Term program = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, spec);
            Term actual = await EtsCompiled419Access.Call(
                context,
                EtsCompiled419Names.Run,
                Term.List(Term.I(1), Term.A(EtsCompiled419Names.Tail)),
                program,
                Nil.Value
            );
            EtsCompiled419Access.Require(actual.Equals(Term.List(Term.I(1))));

            return true;
        });
    }

    internal static async Task Trap()
    {
        await using var runtime = new ProcessRuntime(1);
        await EtsCompiled419Access.Initialize(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term program = await EtsCompiled419Access.Call(context, EtsCompiled419Names.Compile, EtsCompiled419Access.TupleSpec);
            Term tail = Term.A(EtsCompiled419Names.Tail);
            foreach (int count in new[] { EtsCompiled419Names.TrapRows, EtsCompiled419Names.TrapRows + 1 })
            {
                Term input = tail;
                for (int index = 1; index <= count; index++) input = new Cons(Term.I(index), input);
                bool failed = false;
                try
                {
                    _ = await EtsCompiled419Access.Call(
                        context,
                        EtsCompiled419Names.Run,
                        input,
                        program,
                        Nil.Value
                    );
                }
                catch (ErlangException error)
                {
                    TupleTerm frame = EtsCompiled419Access.First(error);
                    Term[] args = Cons.Items(frame.Items[2]).ToArray();
                    Term expected = count == EtsCompiled419Names.TrapRows ? input : new Cons(Term.I(1), tail);
                    failed = frame.Items[1].Equals(Term.A(EtsCompiled419Names.Run)) && args.Length == 3
                        && args[0].Equals(expected) && args[1].Equals(program) && args[2] is Nil;
                }
                EtsCompiled419Access.Require(failed);
            }

            return true;
        });
    }
}
