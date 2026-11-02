using System.Reflection;
using System.Runtime.ExceptionServices;
using Erlang.Compiler;
using static Erlang.Tests.CodePurgeOutcome706Names;

namespace Erlang.Tests;

internal static class CodePurgeOutcome706Checks
{
    internal static async Task Run()
    {
        await Check(
            false,
            false,
            0,
            OriginalAbsent
        );
        await Check(
            true,
            false,
            0,
            OriginalAbsent
        );
        await Check(
            true,
            true,
            0,
            OriginalIdle
        );
        await Check(
            true,
            true,
            Victims,
            OriginalHeld
        );
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static object Invoke(
        ProcessRuntime runtime,
        ProcessContext context,
        string method
    )
    {
        MethodInfo entry = typeof(ProcessRuntime).GetMethod(
            method,
            Members,
            null,
            [typeof(ProcessContext), typeof(string)],
            null
        ) ?? throw new InvalidOperationException(Failure);
        try
        {
            return entry.Invoke(runtime, [context, SubjectModule])!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static Task Unwind(ProcessContext context)
        => ((TaskCompletionSource)(typeof(ProcessContext).GetProperty(Unwound, Members)!
            .GetValue(context)!)).Task;

    private static Term Original(string label)
    {
        Term values = OwnedFunctionOriginal407Terms.Decode(CodePurgerOriginal432Access.Resource(CodePurgerOriginal432Names.Values));
        TupleTerm record = Cons.Items(values).OfType<TupleTerm>().Single(value => value.Items[0].Equals(Term.A(label)));
        Require(record.Items[1] is TupleTerm { Items: [Atom { Name: OriginalValue }, _] });

        return ((TupleTerm)record.Items[1]).Items[1];
    }

    private static async Task RunActor(ProcessRuntime runtime, Func<ProcessContext, Task> body)
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actor = runtime.Spawn(async context =>
        {
            try
            {
                await body(context);
                result.TrySetResult();
            }
            catch (Exception error)
            {
                result.TrySetException(error);
                throw;
            }

            return Nil.Value;
        });
        await result.Task.WaitAsync(CodePurgeHostParameters.Deadline);
        await actor.Completion.WaitAsync(CodePurgeHostParameters.Deadline);
    }

    private static async Task Check(
        bool loaded,
        bool makeOld,
        int count,
        string originalCase
    )
    {
        await using var runtime = new ProcessRuntime(ActorLanes);
        var entered = Enumerable.Range(0, count)
            .Select(_ => new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
        var targets = new List<ProcessHandle>(count);
        var contexts = new List<ProcessContext>(count);
        int enteredCount = 0;
        runtime.Modules.Register(
            Callback,
            Block,
            0,
            async (context, _) =>
            {
                int index = Interlocked.Increment(ref enteredCount) - 1;
                Require(index < count);
                entered[index].SetResult(context);
                await context.ReceiveAsync<Term>(message => message);

                return Term.A(ReleasedValue);
            }
        );
        if (loaded) new Parser(OldSource).ParseModule().Register(runtime.Modules);
        try
        {
            for (int index = 0; index < count; index++)
                targets.Add(runtime.Spawn(context => runtime.Modules.Call(context, SubjectModule, RunFunction)));
            foreach (var signal in entered)
                contexts.Add(await signal.Task.WaitAsync(CodePurgeHostParameters.Deadline));
            await RunActor(
                runtime,
                async context =>
            {
                var monitors = targets.Select(target => runtime.Monitor(context, target.Pid)).ToArray();
                if (makeOld)
                {
                    Require(runtime.Modules.DeleteModule(SubjectModule).Equals(Term.A(ErlangBooleanAtoms.True)));
                    new Parser(NewSource).ParseModule().Register(runtime.Modules);
                }
                ModuleCodeImage? current = runtime.Modules.CurrentCodeImage(SubjectModule);
                Require(runtime.Modules.HasOldCode(SubjectModule) == makeOld);
                Require(contexts.All(victim => runtime.Modules.IsExecutingOldCode(victim, SubjectModule)));
                Term actual = await (ValueTask<Term>)Invoke(runtime, context, OutcomeMethod);
                Term expected = Original(originalCase);
                Term reason = Term.A(ProcessExitReasons.Killed);
                if (count != 0)
                {
                    Require(expected is TupleTerm { Items.Count: OriginalHeldArity });
                    var held = (TupleTerm)expected;
                    Require(actual.Equals(held.Items[0]));
                    reason = held.Items[1];
                }
                else
                    Require(actual.Equals(expected));
                // Outcome publication itself must follow every real victim's physical unwind.
                Require(contexts.All(victim => Unwind(victim).IsCompletedSuccessfully));
                for (int index = 0; index < count; index++)
                {
                    var down = await context.ReceiveAsync(message => message is TupleTerm tuple
                        && tuple.Items.Count == DownArity && tuple.Items[0].Equals(Term.A(Down))
                        && tuple.Items[1].Equals(monitors[index]) ? tuple : null);
                    Require(down!.Items[2].Equals(Term.A(ProcessTag)) && down.Items[3].Equals(targets[index].Pid)
                        && down.Items[4].Equals(reason));
                    Require((await targets[index].Completion.WaitAsync(CodePurgeHostParameters.Deadline)).Equals(reason));
                    await Unwind(contexts[index]).WaitAsync(CodePurgeHostParameters.Deadline);
                    Require(!runtime.Modules.IsExecutingOldCode(contexts[index], SubjectModule));
                }
                Require(!runtime.Modules.HasOldCode(SubjectModule));
                Require(ReferenceEquals(current, runtime.Modules.CurrentCodeImage(SubjectModule)));
                if (makeOld)
                    Require((await runtime.Modules.Call(context, SubjectModule, RunFunction)).Equals(Term.A(CurrentValue)));
                // Preserve the old internal bool contract: successful idle image removal is true.
                if (makeOld && count == 0)
                {
                    Require(runtime.Modules.DeleteModule(SubjectModule).Equals(Term.A(ErlangBooleanAtoms.True)));
                    Require(await (ValueTask<bool>)Invoke(runtime, context, LegacyMethod));
                    Require(!runtime.Modules.HasOldCode(SubjectModule) && !runtime.Modules.IsModuleLoaded(SubjectModule));
                    Require(!await (ValueTask<bool>)Invoke(runtime, context, LegacyMethod));
                }
            }
            );
        }
        finally
        {
            foreach (var target in targets)
                runtime.Send(target.Pid, Term.A(Stop));
        }
    }
}
