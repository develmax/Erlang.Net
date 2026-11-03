using System.Reflection;

namespace Erlang.Tests;

internal static class NativeBindingScopeCases
{
    internal static async Task<IEnumerable<object>> Run()
    {
        var rows = new List<object>();
        await using var runtime = new ProcessRuntime();
        MethodInfo method = typeof(ModuleRegistry).GetMethod(NativeBindingSelectionNames.NativeCall, BindingFlags.Instance | BindingFlags.NonPublic)!;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                async ValueTask<Term> Call(
                    string module,
                    string function,
                    Term[] arguments,
                    NativeBindingScopeProbe probe
                )
                    => await (ValueTask<Term>)method.Invoke(runtime.Modules,
                        [context, module, function, arguments, new Func<ProcessContext, IDisposable>(_ => probe.Enter())])!;
                var success = new NativeBindingScopeProbe();
                Term reversed = await Call(
                    NativeBindingSelectionNames.Lists,
                    NativeBindingSelectionNames.Reverse,
                    [Term.List(Term.I(1), Term.I(2)), Nil.Value],
                    success
                );
                if (!reversed.Equals(Term.List(Term.I(2), Term.I(1))) || success.Entered != 1 || success.Disposed != 1)
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                rows.Add(new { Case = "success", success.Entered, success.Disposed });
                var throwing = new NativeBindingScopeProbe();
                bool caught = false;
                try
                {
                    await Call(
                        NativeBindingSelectionNames.Erlang,
                        NativeBindingSelectionNames.Throw,
                        [Term.A(NativeBindingSelectionNames.ThrowReason)],
                        throwing
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == NativeBindingSelectionNames.Throw && error.Reason.Equals(Term.A(NativeBindingSelectionNames.ThrowReason)))
                {
                    caught = true;
                }
                if (!caught || throwing.Entered != 1 || throwing.Disposed != 1)
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                rows.Add(new { Case = NativeBindingSelectionNames.Throw, throwing.Entered, throwing.Disposed });
                var yielding = new NativeBindingScopeProbe();
                Term yielded = await Call(
                    NativeBindingSelectionNames.Erlang,
                    NativeBindingSelectionNames.Yield,
                    [],
                    yielding
                );
                if (!yielded.Equals(Term.A(NativeBindingSelectionNames.True)) || yielding.Entered != 1 || yielding.Disposed != 1)
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                rows.Add(new { Case = NativeBindingSelectionNames.Yield, yielding.Entered, yielding.Disposed });
                var transfer = new NativeBindingScopeProbe();
                bool transferred = false;
                try
                {
                    await Call(
                        NativeBindingSelectionNames.Erlang,
                        ProcessHibernateNames.Function,
                        [Term.A(NativeBindingSelectionNames.ResumeModule), Term.A(NativeBindingSelectionNames.ResumeFunction), Term.List(Term.I(7))],
                        transfer
                    );
                }
                catch (ProcessHibernateTransfer control)
                {
                    transferred = control.Module == NativeBindingSelectionNames.ResumeModule && control.Function == NativeBindingSelectionNames.ResumeFunction
                        && control.Arguments.Count == 1 && control.Arguments[0].Equals(Term.I(7));
                }
                if (!transferred || transfer.Entered != 1 || transfer.Disposed != 1)
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                rows.Add(new { Case = "hibernate-control-transfer", transfer.Entered, transfer.Disposed });
                var failedFactory = new NativeBindingScopeProbe();
                bool factoryFailed = false;
                int factoryCalls = 0;
                try
                {
                    await (ValueTask<Term>)method.Invoke(
                        runtime.Modules,
                        [context, NativeBindingSelectionNames.Lists, NativeBindingSelectionNames.Reverse, new Term[] { Nil.Value, Nil.Value },
                            new Func<ProcessContext, IDisposable>(_ =>
                            {
                                factoryCalls++;
                                throw new OperationCanceledException(NativeBindingSelectionNames.FactoryCancellation);
                            })]
                    )!;
                }
                catch (OperationCanceledException)
                {
                    factoryFailed = true;
                }
                if (!factoryFailed || factoryCalls != 1 || failedFactory.Entered != 0 || failedFactory.Disposed != 0)
                    throw new InvalidDataException(NativeBindingSelectionNames.Failure);
                rows.Add(new { Case = "factory-cancel", failedFactory.Entered, failedFactory.Disposed });
                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(NativeBindingSelectionNames.Deadline);
        await actor.Completion.WaitAsync(NativeBindingSelectionNames.Deadline);

        return rows;
    }
}
