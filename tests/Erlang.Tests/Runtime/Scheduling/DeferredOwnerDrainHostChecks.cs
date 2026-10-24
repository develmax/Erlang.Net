using Erlang;
using System.Linq.Expressions;
using System.Reflection;

namespace Erlang.Tests;

internal static class DeferredOwnerDrainHostChecks
{
    internal static async Task Run()
    {
        await CheckOrderedOutsideGateAndReentrantSnapshot();
        await CheckPriorTerminationDropsPreparation();
        await CheckCrossOwnerActualFunctionFactories();
    }

    private static async Task CheckOrderedOutsideGateAndReentrantSnapshot()
    {
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext owner = await receiver.Ready;
        object runtimeGate = Gate(runtime), mailboxGate = Gate(owner.Mailbox);
        var sequence = new List<string>();
        int released = 0;
        Pending(owner, () =>
 {
     sequence.Add(DeferredOwnerDrainHostNames.Prefix);

     return null;
 });
        Queue(
            runtime,
            owner,
            () =>
        {
            Check(!Monitor.IsEntered(runtimeGate) && !Monitor.IsEntered(mailboxGate));
            Check(owner.Mailbox.Count == 0);
            sequence.Add(DeferredOwnerDrainHostNames.PrepareFirst);

            return Admission(Term.A(DeferredOwnerDrainHostNames.First));
        },
            () => released++
        );
        Pending(owner, () =>
 {
     sequence.Add(DeferredOwnerDrainHostNames.Middle);

     return null;
 });
        Queue(
            runtime,
            owner,
            () =>
        {
            Check(!Monitor.IsEntered(runtimeGate) && !Monitor.IsEntered(mailboxGate));
            Check(owner.Mailbox.Count == 1);
            sequence.Add(DeferredOwnerDrainHostNames.PrepareSecond);

            return Admission(Term.A(DeferredOwnerDrainHostNames.Second));
        },
            () => released++
        );
        Term? first = await receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(
            message => message, DeferredOwnerDrainHostNames.Deadline));
        Term? second = await receiver.InvokeAsync(async context => await context.ReceiveAsync<Term>(
            message => message, DeferredOwnerDrainHostNames.Deadline));
        Check(first!.Equals(Term.A(DeferredOwnerDrainHostNames.First)));
        Check(second!.Equals(Term.A(DeferredOwnerDrainHostNames.Second)));
        Check(sequence.SequenceEqual(new[] { DeferredOwnerDrainHostNames.Prefix,
            DeferredOwnerDrainHostNames.PrepareFirst, DeferredOwnerDrainHostNames.Middle,
            DeferredOwnerDrainHostNames.PrepareSecond }));
        Check(released == 2 && owner.Mailbox.Count == 0);
    }

    private static async Task CheckPriorTerminationDropsPreparation()
    {
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await using var sender = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext owner = await receiver.Ready;
        await sender.InvokeAsync(async context =>
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Exit,
                owner.Self,
                Term.A(ProcessExitSignals.Kill)
            ));
        int prepared = 0, released = 0;
        Queue(
            runtime,
            owner,
            () =>
        {
            prepared++;

            return Admission(Term.A(DeferredOwnerDrainHostNames.First));
        },
            () => released++
        );
        _ = receiver.InvokeAsync(async context =>
        {
            _ = await context.ReceiveAsync<Term>(message => message, DeferredOwnerDrainHostNames.Deadline);

            return true;
        });
        Term result = await receiver.Process.Completion.WaitAsync(DeferredOwnerDrainHostNames.Deadline);
        Check(result.Equals(Term.A(ProcessExitReasons.Killed)));
        Check(prepared == 0 && released == 1);
    }

    private static async Task CheckCrossOwnerActualFunctionFactories()
    {
        await using var runtime = new ProcessRuntime();
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext ownerA = await first.Ready, ownerB = await second.Ready;
        using var barrier = new Barrier(2);
        bool observe = false;

        byte[] Encode(string module, ProcessContext creator, ProcessContext other)
        {
            var identity = new LocalFunctionIdentity(
                Term.A(module),
                null,
                DeferredOwnerDrainHostNames.FunctionIndex,
                DeferredOwnerDrainHostNames.FunctionOldUnique,
                new BitString(new byte[DeferredOwnerDrainHostNames.FunctionUniqueBytes])
            );
            runtime.Functions.Register(
                identity,
                DeferredOwnerDrainHostNames.FunctionArity,
                0,
                environment =>
            {
                if (observe)
                {
                    Check(!Monitor.IsEntered(Gate(runtime)) && !Monitor.IsEntered(Gate(creator.Mailbox)));
                    Check(barrier.SignalAndWait(DeferredOwnerDrainHostNames.Deadline));
                    // Public registered owner RPC, not a synthetic fake snapshot.
                    _ = runtime.Modules.Call(
                        creator,
                        ErlangModuleNames.Module,
                        ProcessInfoNames.Function,
                        other.Self,
                        Term.A(ProcessInfoNames.Messages)
                    ).GetAwaiter().GetResult();
                }

                return new FunctionTerm(
                    identity,
                    DeferredOwnerDrainHostNames.FunctionArity,
                    environment,
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
            }
            );
            FunctionTerm value = runtime.Functions.Restore(
                runtime,
                identity,
                DeferredOwnerDrainHostNames.FunctionArity,
                []
            );

            return ExternalTermFormat.Encode(value, new EtfEncodingOptions { FunctionOwner = creator.Self });
        }
        byte[] bytesA = Encode(DeferredOwnerDrainHostNames.FunctionA, ownerA, ownerB);
        byte[] bytesB = Encode(DeferredOwnerDrainHostNames.FunctionB, ownerB, ownerA);
        observe = true;
        int released = 0;
        Queue(
            runtime,
            ownerA,
            () => Admission(runtime.ImportExternalTerm(bytesA).Term),
            () => Interlocked.Increment(ref released)
        );
        Queue(
            runtime,
            ownerB,
            () => Admission(runtime.ImportExternalTerm(bytesB).Term),
            () => Interlocked.Increment(ref released)
        );

        Task<string?> Drain(ProcessContext owner) => Task.Run<string?>(() =>
        {
            try
            {
                _ = owner.Mailbox.Count;

                return null;
            }
            catch (Exception exception)
            {
                return exception.GetType().FullName;
            }
        });
        string?[] outcomes = await Task.WhenAll(Drain(ownerA), Drain(ownerB)).WaitAsync(DeferredOwnerDrainHostNames.Deadline);
        Check(outcomes.Any(value => value == DeferredOwnerDrainHostNames.CycleException));
        Check(outcomes.All(value => value is null || value == DeferredOwnerDrainHostNames.CycleException));
        Check(released == 2);
        Check(!first.Process.Completion.IsCompleted && !second.Process.Completion.IsCompleted);
    }

    private static object Gate(object owner)
        => owner.GetType().GetField(DeferredOwnerDrainHostNames.Gate, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static object Admission(Term message)
        => typeof(ProcessRuntime).Assembly.GetType(DeferredOwnerDrainHostNames.AdmissionType, true)!
            .GetConstructor([typeof(Term), typeof(bool)])!.Invoke([message, false]);

    private static void Queue(
        ProcessRuntime runtime,
        ProcessContext owner,
        Func<object?> preparation,
        Action release
    )
    {
        MethodInfo method = typeof(ProcessRuntime).GetMethod(DeferredOwnerDrainHostNames.Queue,
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Type delegateType = method.GetParameters()[1].ParameterType;
        Type resultType = delegateType.GetGenericArguments()[0];
        Delegate exact = Expression.Lambda(delegateType,
            Expression.Convert(Expression.Invoke(Expression.Constant(preparation)), resultType)).Compile();
        method.Invoke(runtime, [owner, exact, release, null, null]);
    }

    private static void Pending(ProcessContext owner, Func<bool?> callback)
        => typeof(Mailbox).GetMethod(DeferredOwnerDrainHostNames.SendPending,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner.Mailbox,
                [Nil.Value, callback, null, false, null, true, null]);

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(DeferredOwnerDrainHostNames.Failure);
    }
}
