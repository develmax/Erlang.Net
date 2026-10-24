namespace Erlang.Tests;

internal static class EtsCompiled419Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(EtsCompiled419Names.Failure);
    }

    internal static Task Initialize(ProcessRuntime runtime)
        => runtime.InitializeModulesAsync(registry => RuntimeBootstrapEtsCompiled419.Register(runtime, registry)).AsTask();

    internal static ValueTask<Term> Call(ProcessContext context, string function, params Term[] args)
        => context.Runtime.Modules.Call(
            context,
            EtsCompiled419Names.Module,
            function,
            args
        );

    internal static Term IdentitySpec => Term.List(Term.Tuple(Term.A(EtsCompiled419Names.Wildcard), Nil.Value, Term.List(Term.A(EtsCompiled419Names.Whole))));

    internal static Term TupleSpec => Term.List(Term.Tuple(Term.Tuple(Term.A(EtsCompiled419Names.TupleOnly)), Nil.Value, Term.List(Term.A(EtsCompiled419Names.True))));

    internal static ErlangFunction Body(ProcessRuntime runtime, string function, int arity)
        => ((Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(EtsCompiled419Names.Functions, EtsCompiled419Names.Members)!.GetValue(runtime.Modules)!)
            [(EtsCompiled419Names.Module, function, arity)];

    internal static TupleTerm First(ErlangException error)
    {
        Require(error.Reason.Equals(Term.A(EtsCompiled419Names.Badarg)) && error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } });
        var first = (TupleTerm)((Cons)error.StackTraceTerm).Head;
        Require(first.Items[0].Equals(Term.A(EtsCompiled419Names.Module)));

        return first;
    }
}
