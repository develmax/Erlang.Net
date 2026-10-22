using Erlang;

namespace Erlang.Tests;

internal static class EtsMatch411Access
{
    internal static async Task Enable(ProcessRuntime runtime)
    {
        await runtime.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.Register(
            runtime, registry, RuntimeNativePrimitives.EtsStorageCore
        ));
        EtsFoundation405Access.Require(runtime.NativePrimitives.Selection == RuntimeNativePrimitives.EtsStorageCore
            && runtime.NativePrimitives.Bindings.Count == EtsMatch411Names.Bodies
            && runtime.NativePrimitives.Bindings.All(binding => binding.Module == EtsMatch411Names.ModuleName
                && runtime.Modules.SourceOwner(binding.Module, binding.Function, binding.Arity) is null));
    }

    internal static ValueTask<Term> Call(ProcessContext context, string name, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            EtsMatch411Names.ModuleName,
            name,
            arguments
        );

    internal static async Task BadArgument(Func<ValueTask<Term>> operation)
    {
        bool caught = false;
        try
        {
            _ = await operation();
        }
        catch (ErlangException error)
        {
            caught = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))
                && error.StackTraceTerm is Cons { Head: TupleTerm { Items.Count: 4 } frame }
                && frame.Items[0].Equals(Term.A(EtsMatch411Names.ModuleName))
                && frame.Items[1].Equals(Term.A(EtsMatch411Names.Function))
                && Cons.Items(frame.Items[2]).Count() == 2;
        }
        EtsFoundation405Access.Require(caught);
    }
}
