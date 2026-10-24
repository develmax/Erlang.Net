namespace Erlang.Tests;

internal static class BinaryWrappersHostChecks
{
    internal static async Task CallbackEffectsAndOptions()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            int calls = 0;
            var replacement = new FunctionTerm(1, (_, a) =>
            {
                calls++;

                return ValueTask.FromResult<Term>(calls == 1 ? Term.A(BinaryWrappersHostNames.InvalidIodata) : a[0]);
            });
            Term subject = new BitString(new byte[] { 1, 2, 1 });
            Term pattern = new BitString(new byte[] { 1 });
            try
            {
                await runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Replace,
                    [subject, pattern, replacement, Term.List(Term.A(BinaryWrappersHostNames.Global))]
                );
                throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                if (calls != 2)
                    throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
            }
            var identity = new FunctionTerm(1, (_, a) => ValueTask.FromResult(a[0]));
            Term ignoredInsert = Term.List(Term.Tuple(Term.A(BinaryWrappersHostNames.InsertReplaced), Term.A(BinaryWrappersHostNames.InvalidIodata)));
            Term result = await runtime.Modules.Call(
                context,
                BinaryModuleNames.Module,
                BinaryModuleNames.Replace,
                [subject, pattern, identity, ignoredInsert]
            );
            if (!result.Equals(subject))
                throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
            var throwing = new FunctionTerm(1, (_, _) => throw new ErlangException(Term.A(BinaryWrappersHostNames.BadOption), ErlangExceptionClasses.Throw));
            try
            {
                await runtime.Modules.Call(
                    context,
                    BinaryModuleNames.Module,
                    BinaryModuleNames.Replace,
                    [subject, pattern, throwing, Nil.Value]
                );
                throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                var frame = (TupleTerm)((Cons)error.StackTraceTerm).Head;
                var annotation = Cons.Items(frame.Items[3]).Cast<TupleTerm>().Single(value => value.Items[0] is Atom { Name: BinaryWrappersHostNames.ErrorInfo });
                var info = (MapTerm)annotation.Items[1];
                if (!info.TryGet(Term.A(BinaryWrappersHostNames.Cause), out Term? cause)
                    || cause is not Atom { Name: BinaryWrappersHostNames.BadOption })
                    throw new InvalidOperationException(BinaryWrappersHostNames.Failure);
            }

            return Nil.Value;
        });
    }
}
