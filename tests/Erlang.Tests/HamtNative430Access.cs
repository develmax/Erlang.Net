namespace Erlang.Tests;

internal static class HamtNative430Access
{
    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(HamtNative430Names.Failure);
    }

    internal static Task<Term> Call(
        RuntimeNodeFoundationHostActor actor,
        string module,
        string function,
        params Term[] args
    )
        => actor.InvokeAsync(async context => module == HamtNative430Names.Debug && function == HamtNative430Names.Same
            ? await DebugSame420HostAccess.Capture(context.Runtime)(context, args)
            : await context.Runtime.Modules.Call(
                context,
                module,
                function,
                args
            ));

    internal static Task<Term> Type(RuntimeNodeFoundationHostActor actor, Term value)
        => Call(
            actor,
            HamtNative430Names.Internal,
            HamtNative430Names.Type,
            value
        );

    internal static async Task Bad(
        RuntimeNodeFoundationHostActor actor,
        string module,
        string function,
        Term value,
        Term reason
    )
    {
        try
        {
            await Call(
                actor,
                module,
                function,
                value
            );
        }
        catch (ErlangException error)
        {
            Require(error.Reason.Equals(reason));
            TupleTerm frame = (TupleTerm)Cons.Items(error.StackTraceTerm).First();
            Require(frame.Items[0].Equals(Term.A(module)) && frame.Items[1].Equals(Term.A(function))
                && frame.Items[2].Equals(Term.List(value)));

            return;
        }
        throw new InvalidOperationException(HamtNative430Names.Failure);
    }
}
