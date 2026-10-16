using Erlang;
using Erlang.Differential;

namespace Erlang.Tests;

internal static class SpawnAsyncDistributionHostChecks
{
    internal static async Task Run()
    {
        await RunInitialization();
        await RunValidation();
        await RunDeadParent();
    }

    internal static async Task RunInitialization()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                SpawnAsyncDistributionHostNames.ProcessFlag,
                Term.A(SpawnAsyncDistributionHostNames.Flag),
                Term.A(ErlangBooleanAtoms.True)
            );
            await FirstInstruction(
                context,
                false,
                Nil.Value,
                false
            );
            await FirstInstruction(
                context,
                false,
                Term.List(Option(true)),
                true
            );
            await FirstInstruction(
                context,
                false,
                Term.List(Option(true), Option(false)),
                false
            );
            await FirstInstruction(
                context,
                false,
                Term.List(Option(false), Option(true)),
                true
            );
            await FirstInstruction(
                context,
                true,
                Term.List(Option(true)),
                true
            );
            await FirstInstruction(
                context,
                true,
                Term.List(Option(true), Option(false)),
                false
            );

            return Nil.Value;
        });
    }

    private static async Task FirstInstruction(
        ProcessContext owner,
        bool request,
        Term options,
        bool expected
    )
    {
        var runtime = owner.Runtime;
        var first = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new FunctionTerm(
            0,
            async (execution, _) =>
        {
            var child = (ProcessContext)execution;
            try
            {
                entered.TrySetResult(child);
                first.TrySetResult(await runtime.Modules.Call(
                    child,
                    ErlangModuleNames.Module,
                    SpawnAsyncDistributionHostNames.Information,
                    child.Self,
                    Term.A(SpawnAsyncDistributionHostNames.Flag)
                ));
                await release.Task.WaitAsync(child.Cancellation);

                return Nil.Value;
            }
            catch (Exception failure)
            {
                first.TrySetException(failure);
                throw;
            }
            finally
            {
                unwound.TrySetResult();
            }
        }
        );
        Term actualOptions = Cons.From(Cons.Items(options).Append(Term.A(SpawnAsyncDistributionHostNames.Monitor)));
        ProcessContext? childContext = null;
        ReferenceTerm? reference = null;
        try
        {
            Term result = await runtime.Modules.Call(
                owner,
                ErlangModuleNames.Module,
                request ? SpawnAsyncDistributionHostNames.Request : SpawnAsyncDistributionHostNames.Spawn,
                callback,
                actualOptions
            );
            if (request)
                reference = (ReferenceTerm)result;
            else
            {
                var pair = (TupleTerm)result;
                reference = (ReferenceTerm)pair.Items[1];
            }
            childContext = await entered.Task.WaitAsync(SpawnAsyncDistributionHostNames.Deadline);
            Require((await first.Task.WaitAsync(SpawnAsyncDistributionHostNames.Deadline)).Equals(Term.Tuple(
                Term.A(SpawnAsyncDistributionHostNames.Flag), Term.A(expected ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False))));
            Require(!unwound.Task.IsCompleted);
            Require((await runtime.Modules.Call(
                owner,
                ErlangModuleNames.Module,
                SpawnAsyncDistributionHostNames.Information,
                childContext.Self,
                Term.A(SpawnAsyncDistributionHostNames.Flag)
            )).Equals(await first.Task));
            if (request)
            {
                Term? reply = await owner.ReceiveAsync<Term>(
                    term => term is TupleTerm { Items: [Atom { Name: SpawnAsyncDistributionHostNames.Reply }, var replyReference, _, _] } && replyReference.Equals(reference) ? term : null,
                    SpawnAsyncDistributionHostNames.Deadline
                );
                Require(reply!.Equals(Term.Tuple(
                    Term.A(SpawnAsyncDistributionHostNames.Reply),
                    reference,
                    Term.A(SpawnAsyncDistributionHostNames.Success),
                    childContext.Self
                )));
            }
        }
        finally
        {
            release.TrySetResult();
            if (childContext is not null)
            {
                await unwound.Task.WaitAsync(SpawnAsyncDistributionHostNames.Deadline);
                Term? down = await owner.ReceiveAsync<Term>(
                    term => term is TupleTerm { Items: [Atom { Name: SpawnAsyncDistributionHostNames.Down }, var downReference, _, _, _] } && downReference.Equals(reference) ? term : null,
                    SpawnAsyncDistributionHostNames.Deadline
                );
                Require(down!.Equals(Term.Tuple(
                    Term.A(SpawnAsyncDistributionHostNames.Down),
                    reference!,
                    Term.A(SpawnAsyncDistributionHostNames.Process),
                    childContext.Self,
                    Term.A(SpawnAsyncDistributionHostNames.Normal)
                )));
                Require(!runtime.IsAlive(childContext.Self));
            }
        }
    }

    internal static async Task RunValidation()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        int executions = 0;
        var callback = new FunctionTerm(0, (_, _) =>
        {
            Interlocked.Increment(ref executions);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        await owner.InvokeAsync(async context =>
        {
            Term invalid = Term.Tuple(Term.A(SpawnAsyncDistributionHostNames.Flag), Term.A(SpawnAsyncDistributionHostNames.Invalid));
            foreach (Term options in new[] { Term.List(invalid, Option(true)), Term.List(Option(true), invalid), Term.List(Term.A(SpawnAsyncDistributionHostNames.Flag)), new Cons(Option(true), Term.A(SpawnAsyncDistributionHostNames.Invalid)) })
            {
                bool rejected = false;
                try
                {
                    await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        SpawnAsyncDistributionHostNames.Spawn,
                        callback,
                        options
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(SpawnAsyncDistributionHostNames.BadArgument)))
                {
                    rejected = true;
                }
                Require(rejected);
            }
            Require(Volatile.Read(ref executions) == 0);

            return Nil.Value;
        });
    }

    internal static async Task RunDeadParent()
    {
        await using var runtime = new ProcessRuntime();
        var captured = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dead = runtime.Spawn(context =>
        {
            captured.TrySetResult(context);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        ProcessContext parent = await captured.Task.WaitAsync(SpawnAsyncDistributionHostNames.Deadline);
        Require((await dead.Completion.WaitAsync(SpawnAsyncDistributionHostNames.Deadline)).Equals(Term.A(SpawnAsyncDistributionHostNames.Normal)));
        int executions = 0;
        var callback = new FunctionTerm(0, (_, _) =>
        {
            Interlocked.Increment(ref executions);

            return ValueTask.FromResult<Term>(Nil.Value);
        });
        foreach (string function in new[] { SpawnAsyncDistributionHostNames.Spawn, SpawnAsyncDistributionHostNames.Request })
        {
            bool rejected = false;
            try
            {
                await runtime.Modules.Call(
                    parent,
                    ErlangModuleNames.Module,
                    function,
                    callback,
                    Term.List(Option(true))
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A(SpawnAsyncDistributionHostNames.NoProcess)))
            {
                rejected = true;
            }
            Require(rejected);
        }
        Require(Volatile.Read(ref executions) == 0);
    }

    private static Term Option(bool value) => Term.Tuple(Term.A(SpawnAsyncDistributionHostNames.Flag), Term.A(value ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(SpawnAsyncDistributionHostNames.Failure);
    }
}
